using POAMs.Web.Models.ViewModels;

namespace POAMs.Web.Services;

public interface IDataImportService
{
    /// <summary>
    /// Get current data counts for the import dashboard
    /// </summary>
    Task<DataImportDashboardViewModel> GetDashboardDataAsync();

    /// <summary>
    /// Import NIST 800-53 catalog from JSON file stream
    /// </summary>
    Task<ImportResultViewModel> ImportNISTCatalogAsync(Stream fileStream, bool replaceExisting = false);

    /// <summary>
    /// Import Documentation Matrix from CSV file stream
    /// </summary>
    Task<ImportResultViewModel> ImportDocumentationMatrixAsync(Stream fileStream, bool replaceExisting = false);
}
