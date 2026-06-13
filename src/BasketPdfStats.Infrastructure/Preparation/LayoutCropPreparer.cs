using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using BasketPdfStats.Core.Models;

namespace BasketPdfStats.Infrastructure.Preparation;

public sealed class LayoutCropPreparer : ILayoutCropPreparer
{
    public async Task<LayoutCropPreparationResult> PrepareAsync(OcrProcessingRequest request, LayoutCropOptions options, CancellationToken cancellationToken = default)
    {
        var outputFolder = LayoutCropPaths.DocumentCropFolder(request, options);
        var metadataPath = Path.Combine(outputFolder, "crop-metadata.json");
        Directory.CreateDirectory(outputFolder);
        Directory.CreateDirectory(LayoutCropPaths.DocumentImageFolder(request, options));
        Directory.CreateDirectory(LayoutCropPaths.DocumentDebugFolder(request, options));
        var wordBoxesPath = LayoutCropPaths.FullPageWordBoxesPath(request, options);
        if (options.RequireFullPageGeometry && !File.Exists(wordBoxesPath))
        {
            return Failed($"Full-page OCR geometry not found for layout calibration: {wordBoxesPath}", outputFolder, metadataPath);
        }

        var python = ResolveExecutable(options);
        if (string.IsNullOrWhiteSpace(python) || (!IsCommandName(python) && !File.Exists(python)))
        {
            return Failed($"Layout crop Python executable not found: {python}", outputFolder, metadataPath);
        }

        var workingDirectory = options.ResolvePath(options.WorkingDirectory);
        if (!Directory.Exists(workingDirectory))
        {
            return Failed($"Layout crop working directory not found: {workingDirectory}", outputFolder, metadataPath);
        }

        var stopwatch = Stopwatch.StartNew();
        var result = await RunAsync(
            python,
            BuildArguments(request, options),
            workingDirectory,
            TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds)),
            cancellationToken);
        stopwatch.Stop();
        if (!result.Started)
        {
            return Failed(result.ErrorMessage ?? "Layout crop preparer did not start.", outputFolder, metadataPath, stopwatch.ElapsedMilliseconds);
        }

        if (result.TimedOut)
        {
            return Failed($"Layout crop preparer timed out. {result.Stderr} {result.Stdout}".Trim(), outputFolder, metadataPath, stopwatch.ElapsedMilliseconds);
        }

        if (result.ExitCode != 0)
        {
            return Failed($"Layout crop preparer failed with exit code {result.ExitCode}. {result.Stderr} {result.Stdout}".Trim(), outputFolder, metadataPath, stopwatch.ElapsedMilliseconds);
        }

        if (!File.Exists(metadataPath))
        {
            return Failed($"Layout crop preparer completed but did not create metadata: {metadataPath}", outputFolder, metadataPath, stopwatch.ElapsedMilliseconds);
        }

        return HasUsableCalibration(metadataPath)
            ? new LayoutCropPreparationResult { Success = true, OutputFolder = outputFolder, MetadataPath = metadataPath, DurationMs = stopwatch.ElapsedMilliseconds }
            : Failed($"Layout calibration did not produce usable crops. Inspect metadata and calibration report: {metadataPath}", outputFolder, metadataPath, stopwatch.ElapsedMilliseconds);
    }

    public string BuildArguments(OcrProcessingRequest request, LayoutCropOptions options)
    {
        var inputPath = string.IsNullOrWhiteSpace(request.WorkingPath) ? request.PdfPath : request.WorkingPath;
        return $"-m {options.WorkerModule}" +
               $" --pdf {Quote(inputPath)}" +
               $" --layout-map {Quote(options.ResolvePath(options.LayoutMapPath))}" +
               $" --calibration-rules {Quote(options.ResolvePath(options.CalibrationRulesPath))}" +
               $" --word-boxes {Quote(LayoutCropPaths.FullPageWordBoxesPath(request, options))}" +
               $" --output-folder {Quote(LayoutCropPaths.DocumentCropFolder(request, options))}" +
               $" --image-output-folder {Quote(LayoutCropPaths.DocumentImageFolder(request, options))}" +
               $" --layout-debug-folder {Quote(LayoutCropPaths.DocumentDebugFolder(request, options))}" +
               " --input-mode Crops" +
               $" --max-pages {options.MaxPages}" +
               $" --document-hash {Quote(request.DocumentHash)}" +
               $" --original-file-name {Quote(request.OriginalFileName)}";
    }

    private static string ResolveExecutable(LayoutCropOptions options)
    {
        return IsCommandName(options.PythonExecutablePath)
            ? options.PythonExecutablePath
            : options.ResolvePath(options.PythonExecutablePath);
    }

    private static async Task<ProcessResult> RunAsync(string executablePath, string arguments, string workingDirectory, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.OutputDataReceived += (_, args) => { if (args.Data is not null) stdout.AppendLine(args.Data); };
        process.ErrorDataReceived += (_, args) => { if (args.Data is not null) stderr.AppendLine(args.Data); };
        try
        {
            if (!process.Start())
            {
                return new ProcessResult { ErrorMessage = "Process did not start." };
            }
        }
        catch (Win32Exception ex)
        {
            return new ProcessResult { ErrorMessage = ex.Message };
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var waitTask = process.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(timeout, cancellationToken);
        if (await Task.WhenAny(waitTask, timeoutTask) == timeoutTask && !process.HasExited)
        {
            TryKill(process);
            return new ProcessResult { Started = true, TimedOut = true, Stdout = stdout.ToString(), Stderr = stderr.ToString() };
        }

        await waitTask;
        return new ProcessResult { Started = true, ExitCode = process.ExitCode, Stdout = stdout.ToString(), Stderr = stderr.ToString() };
    }

    private static LayoutCropPreparationResult Failed(string error, string outputFolder = "", string metadataPath = "", long durationMs = 0)
    {
        return new LayoutCropPreparationResult { OutputFolder = outputFolder, MetadataPath = metadataPath, DurationMs = durationMs, Errors = [error] };
    }

    private static bool IsCommandName(string value) =>
        !Path.IsPathRooted(value) && !value.Contains(Path.DirectorySeparatorChar) && !value.Contains(Path.AltDirectorySeparatorChar);

    private static bool HasUsableCalibration(string metadataPath)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(metadataPath));
            var root = document.RootElement;
            if (!root.TryGetProperty("layoutCalibration", out var calibration) ||
                !calibration.TryGetProperty("status", out var status) ||
                status.ValueKind != System.Text.Json.JsonValueKind.String ||
                status.GetString() is not ("Calibrated" or "Partial"))
            {
                return false;
            }

            return root.TryGetProperty("crops", out var crops) &&
                   crops.ValueKind == System.Text.Json.JsonValueKind.Array &&
                   crops.EnumerateArray().Any(crop =>
                       !crop.TryGetProperty("usable", out var usable) ||
                       usable.ValueKind != System.Text.Json.JsonValueKind.False);
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort.
        }
    }

    private sealed class ProcessResult
    {
        public bool Started { get; set; }
        public bool TimedOut { get; set; }
        public int ExitCode { get; set; }
        public string Stdout { get; set; } = string.Empty;
        public string Stderr { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}
