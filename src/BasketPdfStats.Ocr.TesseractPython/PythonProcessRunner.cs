using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace BasketPdfStats.Ocr.TesseractPython;

public sealed class PythonProcessRunner
{
    public async Task<PythonProcessResult> RunAsync(
        string executablePath,
        string arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string?> environment,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var item in environment)
        {
            if (item.Value is not null)
            {
                process.StartInfo.Environment[item.Key] = item.Value;
            }
        }

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                stdout.AppendLine(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                stderr.AppendLine(args.Data);
            }
        };

        try
        {
            if (!process.Start())
            {
                return new PythonProcessResult { Started = false, ErrorMessage = "Process did not start." };
            }
        }
        catch (Win32Exception ex)
        {
            return new PythonProcessResult { Started = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new PythonProcessResult { Started = false, ErrorMessage = ex.Message };
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            var waitTask = process.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(timeout, cancellationToken);
            var completed = await Task.WhenAny(waitTask, timeoutTask);
            if (completed == timeoutTask)
            {
                TryKill(process);
                return new PythonProcessResult
                {
                    Started = true,
                    TimedOut = true,
                    Stdout = stdout.ToString(),
                    Stderr = stderr.ToString(),
                    ErrorMessage = $"Process timed out after {timeout.TotalSeconds:0} seconds."
                };
            }

            await waitTask;
            return new PythonProcessResult
            {
                Started = true,
                ExitCode = process.ExitCode,
                Stdout = stdout.ToString(),
                Stderr = stderr.ToString()
            };
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort cleanup; the caller receives a timeout result.
        }
    }
}
