using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;
using POAMs.Web.Services;

namespace POAMs.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class DataImportController : Controller
{
    private readonly IDataImportService _importService;
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    public DataImportController(
        IDataImportService importService,
        IUserService userService,
        IAuditService auditService)
    {
        _importService = importService;
        _userService = userService;
        _auditService = auditService;
    }

    // GET: DataImport
    public async Task<IActionResult> Index()
    {
        var model = await _importService.GetDashboardDataAsync();
        return View(model);
    }

    // GET: DataImport/NISTCatalog
    public IActionResult NISTCatalog()
    {
        return View(new FileImportViewModel());
    }

    // POST: DataImport/NISTCatalog
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NISTCatalog(FileImportViewModel model)
    {
        if (model.File == null || model.File.Length == 0)
        {
            ModelState.AddModelError("File", "Please select a JSON file to upload.");
            return View(model);
        }

        // Validate file extension
        var extension = Path.GetExtension(model.File.FileName).ToLowerInvariant();
        if (extension != ".json")
        {
            ModelState.AddModelError("File", "Only JSON files are accepted for NIST Catalog import.");
            return View(model);
        }

        using var stream = model.File.OpenReadStream();
        var result = await _importService.ImportNISTCatalogAsync(stream, model.ReplaceExisting);

        // Log the import
        var currentUser = await GetCurrentUserAsync();
        await _auditService.LogAsync(
            currentUser?.Id,
            "Import",
            "NISTCatalog",
            0,
            null,
            new { FileName = model.File.FileName, result.RecordsAdded, result.RecordsSkipped, result.Success },
            GetIpAddress());

        return View("ImportResult", result);
    }

    // GET: DataImport/DocumentationMatrix
    public IActionResult DocumentationMatrix()
    {
        return View(new FileImportViewModel());
    }

    // POST: DataImport/DocumentationMatrix
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DocumentationMatrix(FileImportViewModel model)
    {
        if (model.File == null || model.File.Length == 0)
        {
            ModelState.AddModelError("File", "Please select a CSV file to upload.");
            return View(model);
        }

        // Validate file extension
        var extension = Path.GetExtension(model.File.FileName).ToLowerInvariant();
        if (extension != ".csv")
        {
            ModelState.AddModelError("File", "Only CSV files are accepted for Documentation Matrix import.");
            return View(model);
        }

        using var stream = model.File.OpenReadStream();
        var result = await _importService.ImportDocumentationMatrixAsync(stream, model.ReplaceExisting);

        // Log the import
        var currentUser = await GetCurrentUserAsync();
        await _auditService.LogAsync(
            currentUser?.Id,
            "Import",
            "DocumentationMatrix",
            0,
            null,
            new { FileName = model.File.FileName, result.RecordsAdded, result.RecordsSkipped, result.Success },
            GetIpAddress());

        return View("ImportResult", result);
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        if (User.Identity?.Name == null) return null;
        return await _userService.GetByUsernameAsync(User.Identity.Name);
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
