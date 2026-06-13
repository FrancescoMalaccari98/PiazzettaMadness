namespace BasketPdfStats.Infrastructure.Configuration;

public sealed class RuntimeOptions
{
    public string RuntimeRoot { get; set; } = ".";
    public string InputFolder { get; set; } = "Input";
    public string WorkingFolder { get; set; } = "Working";
    public string OutputJsonFolder { get; set; } = "OutputJson";
    public string ProcessedFolder { get; set; } = "Elaborati";
    public string ErrorFolder { get; set; } = "Errori";
    public string LogsFolder { get; set; } = "Logs";
    public string ConfigFolder { get; set; } = "Config";
    public string DatasetFolder { get; set; } = "Dataset";
    public string NormalizedOcrFolder { get; set; } = "NormalizedOcr";
    public string DebugFolder { get; set; } = "Debug";
    public string ValidationDebugFolder { get; set; } = "Validation";

    public string Resolve(string folder)
    {
        return Path.GetFullPath(Path.Combine(RuntimeRoot, folder));
    }

    public string InputPath => Resolve(InputFolder);
    public string WorkingPath => Resolve(WorkingFolder);
    public string OutputJsonPath => Resolve(OutputJsonFolder);
    public string ProcessedPath => Resolve(ProcessedFolder);
    public string ErrorPath => Resolve(ErrorFolder);
    public string LogsPath => Resolve(LogsFolder);
    public string ConfigPath => Resolve(ConfigFolder);
    public string DatasetPath => Resolve(DatasetFolder);
    public string NormalizedOcrPath => Path.Combine(DatasetPath, NormalizedOcrFolder);
    public string DebugPath => Resolve(DebugFolder);
    public string ValidationDebugPath => Path.Combine(DebugPath, ValidationDebugFolder);
    public string ProcessedIndexPath => Path.Combine(OutputJsonPath, "processed-index.json");
}
