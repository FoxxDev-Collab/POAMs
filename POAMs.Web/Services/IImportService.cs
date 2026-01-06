using POAMs.Web.Models.Domain;

namespace POAMs.Web.Services;

public class ImportResult
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int UpdatedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public interface IImportService
{
    Task<ImportResult> ImportFromExcelAsync(Stream fileStream, int systemId, bool updateExisting = false);
    Task<List<Dictionary<string, string>>> PreviewExcelAsync(Stream fileStream);
}
