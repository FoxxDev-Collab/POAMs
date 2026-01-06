using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using POAMs.Web.Services;

namespace POAMs.Web.Controllers;

[Authorize(Policy = "CanView")]
public class STPController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ISTPService _stpService;
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    public STPController(
        ApplicationDbContext context,
        ISTPService stpService,
        IUserService userService,
        IAuditService auditService)
    {
        _context = context;
        _stpService = stpService;
        _userService = userService;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(int? systemId, STPStatus? status, STPType? type)
    {
        var stps = await _stpService.GetAllAsync(type);

        if (systemId.HasValue)
            stps = stps.Where(s => s.SystemId == systemId.Value).ToList();

        if (status.HasValue)
            stps = stps.Where(s => s.Status == status.Value).ToList();

        ViewBag.Systems = new SelectList(
            await _context.Systems.OrderBy(s => s.SystemName).ToListAsync(),
            "Id", "SystemName", systemId);
        ViewBag.SelectedStatus = status;
        ViewBag.SystemId = systemId;
        ViewBag.CurrentType = type;

        return View(stps);
    }

    public async Task<IActionResult> Details(int id)
    {
        var stp = await _stpService.GetByIdWithTestCasesAsync(id);
        if (stp == null)
        {
            TempData["Error"] = "Security Test Plan not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(stp);
    }

    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Create(int? poamId, int? systemId, STPType? type)
    {
        await PopulateDropdowns();

        var stp = new SecurityTestPlan
        {
            TestPlanDate = DateTime.UtcNow,
            TestPlanVersion = "1.0",
            Classification = "Unclassified",
            Type = type ?? STPType.STIG
        };

        ViewBag.SelectedType = stp.Type;

        if (poamId.HasValue)
        {
            var poam = await _context.POAMs
                .Include(p => p.System)
                .FirstOrDefaultAsync(p => p.Id == poamId.Value);

            if (poam != null)
            {
                stp.POAMId = poamId;
                stp.SystemId = poam.SystemId;
                ViewBag.POAM = poam;
            }
        }
        else if (systemId.HasValue)
        {
            stp.SystemId = systemId.Value;
        }

        return View(stp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Create(SecurityTestPlan stp)
    {
        if (ModelState.IsValid)
        {
            var currentUser = await GetCurrentUserAsync();
            stp.CreatedById = currentUser?.Id;

            await _stpService.CreateAsync(stp);

            await _auditService.LogAsync(
                currentUser?.Id, "Create", "SecurityTestPlan", stp.Id,
                null, stp, GetIpAddress());

            TempData["Success"] = $"Security Test Plan {stp.STPIdentifier} created successfully.";
            return RedirectToAction(nameof(Details), new { id = stp.Id });
        }

        await PopulateDropdowns();
        return View(stp);
    }

    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Edit(int id)
    {
        var stp = await _stpService.GetByIdAsync(id);
        if (stp == null)
        {
            TempData["Error"] = "Security Test Plan not found.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdowns();
        return View(stp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Edit(int id, SecurityTestPlan stp)
    {
        if (id != stp.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            // Use AsNoTracking to avoid EF tracking conflicts when updating
            var oldStp = await _context.SecurityTestPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
            var currentUser = await GetCurrentUserAsync();

            stp.ModifiedDate = DateTime.UtcNow;
            await _stpService.UpdateAsync(stp);

            await _auditService.LogAsync(
                currentUser?.Id, "Update", "SecurityTestPlan", stp.Id,
                oldStp, stp, GetIpAddress());

            TempData["Success"] = "Security Test Plan updated successfully.";
            return RedirectToAction(nameof(Details), new { id = stp.Id });
        }

        await PopulateDropdowns();
        return View(stp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Delete(int id)
    {
        var stp = await _stpService.GetByIdAsync(id);
        if (stp == null)
        {
            TempData["Error"] = "Security Test Plan not found.";
            return RedirectToAction(nameof(Index));
        }

        var currentUser = await GetCurrentUserAsync();
        await _stpService.DeleteAsync(id);

        await _auditService.LogAsync(
            currentUser?.Id, "Delete", "SecurityTestPlan", id,
            stp, null, GetIpAddress());

        TempData["Success"] = $"Security Test Plan {stp.STPIdentifier} deleted.";
        return RedirectToAction(nameof(Index));
    }

    // Test Cases Management
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> AddTestCase(int stpId)
    {
        var stp = await _stpService.GetByIdAsync(stpId);
        if (stp == null)
            return NotFound();

        ViewBag.STP = stp;
        await PopulateDropdowns();

        return View(new STPTestCase
        {
            SecurityTestPlanId = stpId,
            ExpectedResult = "Compliant per STIG check"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> AddTestCase(STPTestCase testCase)
    {
        if (ModelState.IsValid)
        {
            await _stpService.AddSTIGTestCaseAsync(testCase);
            TempData["Success"] = "Test case added.";
            return RedirectToAction(nameof(Details), new { id = testCase.SecurityTestPlanId });
        }

        var stp = await _stpService.GetByIdAsync(testCase.SecurityTestPlanId);
        ViewBag.STP = stp;
        await PopulateDropdowns();
        return View(testCase);
    }

    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> EditTestCase(int id)
    {
        var testCase = await _stpService.GetSTIGTestCaseByIdAsync(id);
        if (testCase == null)
            return NotFound();

        ViewBag.STP = testCase.SecurityTestPlan;
        await PopulateDropdowns();
        return View(testCase);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> EditTestCase(STPTestCase testCase)
    {
        if (ModelState.IsValid)
        {
            await _stpService.UpdateSTIGTestCaseAsync(testCase);
            TempData["Success"] = "Test case updated.";
            return RedirectToAction(nameof(Details), new { id = testCase.SecurityTestPlanId });
        }

        var stp = await _stpService.GetByIdAsync(testCase.SecurityTestPlanId);
        ViewBag.STP = stp;
        await PopulateDropdowns();
        return View(testCase);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> DeleteTestCase(int id)
    {
        var testCase = await _stpService.GetSTIGTestCaseByIdAsync(id);
        if (testCase == null)
            return NotFound();

        var stpId = testCase.SecurityTestPlanId;
        await _stpService.DeleteSTIGTestCaseAsync(id);

        TempData["Success"] = "Test case deleted.";
        return RedirectToAction(nameof(Details), new { id = stpId });
    }

    // Quick status update via AJAX
    [HttpPost]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> UpdateTestCaseStatus([FromBody] TestCaseStatusUpdate update)
    {
        var currentUser = await GetCurrentUserAsync();
        await _stpService.UpdateSTIGTestCaseStatusAsync(
            update.TestCaseId,
            update.Status,
            update.ActualResult,
            update.Evidence,
            currentUser?.Id);

        return Json(new { success = true });
    }

    // ========== Nessus Test Cases ==========
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> AddNessusTestCase(int stpId)
    {
        var stp = await _stpService.GetByIdAsync(stpId);
        if (stp == null)
            return NotFound();

        ViewBag.STP = stp;
        await PopulateDropdowns();

        return View(new NessusTestCase
        {
            SecurityTestPlanId = stpId,
            ExpectedResult = "Vulnerability no longer detected on rescan"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> AddNessusTestCase(NessusTestCase testCase)
    {
        if (ModelState.IsValid)
        {
            await _stpService.AddNessusTestCaseAsync(testCase);
            TempData["Success"] = "Vulnerability finding added.";
            return RedirectToAction(nameof(Details), new { id = testCase.SecurityTestPlanId });
        }

        var stp = await _stpService.GetByIdAsync(testCase.SecurityTestPlanId);
        ViewBag.STP = stp;
        await PopulateDropdowns();
        return View(testCase);
    }

    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> EditNessusTestCase(int id)
    {
        var testCase = await _stpService.GetNessusTestCaseByIdAsync(id);
        if (testCase == null)
            return NotFound();

        ViewBag.STP = testCase.SecurityTestPlan;
        await PopulateDropdowns();
        return View(testCase);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> EditNessusTestCase(NessusTestCase testCase)
    {
        if (ModelState.IsValid)
        {
            await _stpService.UpdateNessusTestCaseAsync(testCase);
            TempData["Success"] = "Vulnerability finding updated.";
            return RedirectToAction(nameof(Details), new { id = testCase.SecurityTestPlanId });
        }

        var stp = await _stpService.GetByIdAsync(testCase.SecurityTestPlanId);
        ViewBag.STP = stp;
        await PopulateDropdowns();
        return View(testCase);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> DeleteNessusTestCase(int id)
    {
        var testCase = await _stpService.GetNessusTestCaseByIdAsync(id);
        if (testCase == null)
            return NotFound();

        var stpId = testCase.SecurityTestPlanId;
        await _stpService.DeleteNessusTestCaseAsync(id);

        TempData["Success"] = "Vulnerability finding deleted.";
        return RedirectToAction(nameof(Details), new { id = stpId });
    }

    // ========== Security Control Test Cases ==========
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> AddControlTestCase(int stpId)
    {
        var stp = await _stpService.GetByIdAsync(stpId);
        if (stp == null)
            return NotFound();

        ViewBag.STP = stp;
        await PopulateDropdowns();

        return View(new SecurityControlTestCase
        {
            SecurityTestPlanId = stpId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> AddControlTestCase(SecurityControlTestCase testCase)
    {
        if (ModelState.IsValid)
        {
            await _stpService.AddControlTestCaseAsync(testCase);
            TempData["Success"] = "Control test case added.";
            return RedirectToAction(nameof(Details), new { id = testCase.SecurityTestPlanId });
        }

        var stp = await _stpService.GetByIdAsync(testCase.SecurityTestPlanId);
        ViewBag.STP = stp;
        await PopulateDropdowns();
        return View(testCase);
    }

    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> EditControlTestCase(int id)
    {
        var testCase = await _stpService.GetControlTestCaseByIdAsync(id);
        if (testCase == null)
            return NotFound();

        ViewBag.STP = testCase.SecurityTestPlan;
        await PopulateDropdowns();
        return View(testCase);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> EditControlTestCase(SecurityControlTestCase testCase)
    {
        if (ModelState.IsValid)
        {
            await _stpService.UpdateControlTestCaseAsync(testCase);
            TempData["Success"] = "Control test case updated.";
            return RedirectToAction(nameof(Details), new { id = testCase.SecurityTestPlanId });
        }

        var stp = await _stpService.GetByIdAsync(testCase.SecurityTestPlanId);
        ViewBag.STP = stp;
        await PopulateDropdowns();
        return View(testCase);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> DeleteControlTestCase(int id)
    {
        var testCase = await _stpService.GetControlTestCaseByIdAsync(id);
        if (testCase == null)
            return NotFound();

        var stpId = testCase.SecurityTestPlanId;
        await _stpService.DeleteControlTestCaseAsync(id);

        TempData["Success"] = "Control test case deleted.";
        return RedirectToAction(nameof(Details), new { id = stpId });
    }

    // Export to Excel
    public async Task<IActionResult> Export(int id)
    {
        var stp = await _stpService.GetByIdWithTestCasesAsync(id);
        if (stp == null)
            return NotFound();

        var bytes = _stpService.ExportToExcel(stp);
        var fileName = $"STP_{stp.STPIdentifier}_{DateTime.UtcNow:yyyyMMdd}.xlsx";

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // Bulk add test cases (e.g., from STIG Viewer import)
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> BulkAddTestCases(int stpId)
    {
        var stp = await _stpService.GetByIdAsync(stpId);
        if (stp == null)
            return NotFound();

        ViewBag.STP = stp;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> BulkAddTestCases(int stpId, string bulkData)
    {
        var stp = await _stpService.GetByIdAsync(stpId);
        if (stp == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(bulkData))
        {
            TempData["Error"] = "No data provided.";
            return RedirectToAction(nameof(Details), new { id = stpId });
        }

        // Parse tab-separated or CSV data
        var testCases = ParseBulkTestCases(bulkData);
        if (testCases.Any())
        {
            await _stpService.AddSTIGTestCasesAsync(stpId, testCases);
            TempData["Success"] = $"Added {testCases.Count} test cases.";
        }
        else
        {
            TempData["Warning"] = "No valid test cases found in the data.";
        }

        return RedirectToAction(nameof(Details), new { id = stpId });
    }

    private List<STPTestCase> ParseBulkTestCases(string data)
    {
        var testCases = new List<STPTestCase>();
        var lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Skip(1)) // Skip header
        {
            var parts = line.Split('\t');
            if (parts.Length < 3) continue;

            var testCase = new STPTestCase
            {
                VulnId = parts[0].Trim(),
                RuleId = parts.Length > 1 ? parts[1].Trim() : "",
                Title = parts.Length > 2 ? parts[2].Trim() : "",
                Severity = ParseSeverity(parts.Length > 3 ? parts[3] : "CAT II"),
                RuleVersion = parts.Length > 4 ? parts[4].Trim() : "",
                ExpectedResult = "Compliant per STIG check"
            };

            if (!string.IsNullOrEmpty(testCase.VulnId))
                testCases.Add(testCase);
        }

        return testCases;
    }

    private STIGSeverity ParseSeverity(string severity)
    {
        var s = severity.ToUpper().Trim();
        if (s.Contains("I") && !s.Contains("II") && !s.Contains("III"))
            return STIGSeverity.CAT_I;
        if (s.Contains("III"))
            return STIGSeverity.CAT_III;
        return STIGSeverity.CAT_II;
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Systems = new SelectList(
            await _context.Systems.OrderBy(s => s.SystemName).ToListAsync(),
            "Id", "SystemName");

        ViewBag.Users = new SelectList(
            await _context.Users.Where(u => u.IsActive).OrderBy(u => u.DisplayName).ToListAsync(),
            "Id", "DisplayName");

        ViewBag.POAMs = new SelectList(
            await _context.POAMs.OrderBy(p => p.ItemIdentifier).ToListAsync(),
            "Id", "ItemIdentifier");
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

public class TestCaseStatusUpdate
{
    public int TestCaseId { get; set; }
    public TestCaseStatus Status { get; set; }
    public string ActualResult { get; set; } = "";
    public string Evidence { get; set; } = "";
}
