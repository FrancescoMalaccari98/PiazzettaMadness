namespace BasketPdfStats.Infrastructure.Preparation;

public sealed class OcrRunPlanBuilderOptions
{
    public bool TesseractEnabled { get; set; } = true;
    public bool TesseractUseLayoutCrops { get; set; }
    public bool TesseractPreprocessImages { get; set; }
    public string TesseractPreprocessedFolder { get; set; } = "runtime/Dataset/Preprocessed/TesseractCrops";
    public string RuntimeRoot { get; set; } = ".";

    public string ResolvePath(string path) => Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(RuntimeRoot, path));
}
