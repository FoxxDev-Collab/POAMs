using System.ComponentModel.DataAnnotations;

namespace POAMs.Web.Models.ViewModels;

/// <summary>
/// View model for the data import dashboard
/// </summary>
public class DataImportDashboardViewModel
{
    public int NISTControlCount { get; set; }
    public int CCICount { get; set; }
    public int DocumentationRequirementCount { get; set; }
    public DateTime? LastNISTImport { get; set; }
    public DateTime? LastDocMatrixImport { get; set; }
}

/// <summary>
/// View model for file upload imports
/// </summary>
public class FileImportViewModel
{
    [Required(ErrorMessage = "Please select a file to upload")]
    [Display(Name = "Import File")]
    public IFormFile? File { get; set; }

    /// <summary>
    /// Whether to replace existing data or skip duplicates
    /// </summary>
    [Display(Name = "Replace existing data")]
    public bool ReplaceExisting { get; set; }
}

/// <summary>
/// Result of an import operation
/// </summary>
public class ImportResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RecordsProcessed { get; set; }
    public int RecordsAdded { get; set; }
    public int RecordsSkipped { get; set; }
    public int RecordsUpdated { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
