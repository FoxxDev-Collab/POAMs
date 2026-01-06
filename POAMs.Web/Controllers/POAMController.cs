using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;
using POAMs.Web.Services;

namespace POAMs.Web.Controllers;

[Authorize(Policy = "CanView")]
public class POAMController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    public POAMController(ApplicationDbContext context, IUserService userService, IAuditService auditService)
    {
        _context = context;
        _userService = userService;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? filter, string? search, int? systemId, string? status, string? risk)
    {
        var query = _context.POAMs
            .Include(p => p.System)
            .Include(p => p.POC)
            .Include(p => p.Milestones)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter))
        {
            switch (filter.ToLower())
            {
                case "overdue":
                    query = query.Where(p => p.ScheduledCompletionDate < DateTime.UtcNow &&
                                            p.Status != POAMStatus.Completed &&
                                            p.Status != POAMStatus.Closed);
                    break;
                case "high":
                    query = query.Where(p => p.RiskLevel == RiskLevel.High &&
                                            p.Status != POAMStatus.Completed &&
                                            p.Status != POAMStatus.Closed);
                    break;
                case "open":
                    query = query.Where(p => p.Status == POAMStatus.Open || p.Status == POAMStatus.Ongoing);
                    break;
            }
        }

        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            query = query.Where(p => p.ItemIdentifier.ToLower().Contains(search) ||
                                    p.WeaknessOrDeficiency.ToLower().Contains(search) ||
                                    (p.SecurityControl != null && p.SecurityControl.ToLower().Contains(search)));
        }

        if (systemId.HasValue)
        {
            query = query.Where(p => p.SystemId == systemId.Value);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<POAMStatus>(status, out var statusEnum))
        {
            query = query.Where(p => p.Status == statusEnum);
        }

        if (!string.IsNullOrEmpty(risk) && Enum.TryParse<RiskLevel>(risk, out var riskEnum))
        {
            query = query.Where(p => p.RiskLevel == riskEnum);
        }

        var poams = await query.OrderByDescending(p => p.ModifiedDate).ToListAsync();

        // Prepare filter dropdowns
        ViewBag.Systems = new SelectList(await _context.Systems.ToListAsync(), "Id", "SystemName", systemId);
        ViewBag.Statuses = new SelectList(Enum.GetValues<POAMStatus>().Select(s => new { Value = s.ToString(), Text = s.ToString() }), "Value", "Text", status);
        ViewBag.RiskLevels = new SelectList(Enum.GetValues<RiskLevel>().Select(r => new { Value = r.ToString(), Text = r.ToString() }), "Value", "Text", risk);
        ViewBag.CurrentFilter = filter;
        ViewBag.CurrentSearch = search;

        return View(poams);
    }

    public async Task<IActionResult> Details(int id)
    {
        var poam = await _context.POAMs
            .Include(p => p.System)
            .Include(p => p.POC)
            .Include(p => p.Milestones.OrderBy(m => m.MilestoneNumber))
                .ThenInclude(m => m.AssignedTo)
            .Include(p => p.UserAssignments)
                .ThenInclude(ua => ua.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (poam == null)
            return NotFound();

        return View(poam);
    }

    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        var model = new POAMEditViewModel
        {
            ScheduledCompletionDate = DateTime.UtcNow.AddMonths(3),
            OriginalPOAMDate = DateTime.UtcNow,
            Status = POAMStatus.Draft,
            RiskLevel = RiskLevel.Moderate
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Create(POAMEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var poam = new POAM
            {
                SystemId = model.SystemId,
                ItemIdentifier = model.ItemIdentifier,
                WeaknessOrDeficiency = model.WeaknessOrDeficiency,
                SecurityControl = model.SecurityControl,
                POCId = model.POCId,
                ResourcesRequired = model.ResourcesRequired,
                ScheduledCompletionDate = model.ScheduledCompletionDate,
                IdentifiedBy = model.IdentifiedBy,
                RiskLevel = model.RiskLevel,
                EstimatedCost = model.EstimatedCost,
                Status = model.Status,
                Comments = model.Comments,
                OriginalPOAMDate = model.OriginalPOAMDate
            };

            _context.POAMs.Add(poam);
            await _context.SaveChangesAsync();

            var currentUser = await GetCurrentUserAsync();
            await _auditService.LogAsync(currentUser?.Id, "Create", "POAM", poam.Id, null, poam, GetIpAddress());

            TempData["Success"] = $"POAM {poam.ItemIdentifier} created successfully.";
            return RedirectToAction(nameof(Details), new { id = poam.Id });
        }

        await PopulateDropdownsAsync(model.SystemId, model.POCId);
        return View(model);
    }

    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Edit(int id)
    {
        var poam = await _context.POAMs.FindAsync(id);
        if (poam == null)
            return NotFound();

        var model = new POAMEditViewModel
        {
            Id = poam.Id,
            SystemId = poam.SystemId,
            ItemIdentifier = poam.ItemIdentifier,
            WeaknessOrDeficiency = poam.WeaknessOrDeficiency,
            SecurityControl = poam.SecurityControl,
            POCId = poam.POCId,
            ResourcesRequired = poam.ResourcesRequired,
            ScheduledCompletionDate = poam.ScheduledCompletionDate,
            IdentifiedBy = poam.IdentifiedBy,
            RiskLevel = poam.RiskLevel,
            EstimatedCost = poam.EstimatedCost,
            Status = poam.Status,
            Comments = poam.Comments,
            OriginalPOAMDate = poam.OriginalPOAMDate
        };

        await PopulateDropdownsAsync(poam.SystemId, poam.POCId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> Edit(int id, POAMEditViewModel model)
    {
        if (id != model.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            var poam = await _context.POAMs.FindAsync(id);
            if (poam == null)
                return NotFound();

            var oldValues = new
            {
                poam.SystemId,
                poam.ItemIdentifier,
                poam.WeaknessOrDeficiency,
                poam.SecurityControl,
                poam.POCId,
                poam.ResourcesRequired,
                poam.ScheduledCompletionDate,
                poam.IdentifiedBy,
                poam.RiskLevel,
                poam.EstimatedCost,
                poam.Status,
                poam.Comments
            };

            poam.SystemId = model.SystemId;
            poam.ItemIdentifier = model.ItemIdentifier;
            poam.WeaknessOrDeficiency = model.WeaknessOrDeficiency;
            poam.SecurityControl = model.SecurityControl;
            poam.POCId = model.POCId;
            poam.ResourcesRequired = model.ResourcesRequired;
            poam.ScheduledCompletionDate = model.ScheduledCompletionDate;
            poam.IdentifiedBy = model.IdentifiedBy;
            poam.RiskLevel = model.RiskLevel;
            poam.EstimatedCost = model.EstimatedCost;
            poam.Status = model.Status;
            poam.Comments = model.Comments;

            await _context.SaveChangesAsync();

            var currentUser = await GetCurrentUserAsync();
            await _auditService.LogAsync(currentUser?.Id, "Update", "POAM", poam.Id, oldValues, model, GetIpAddress());

            TempData["Success"] = $"POAM {poam.ItemIdentifier} updated successfully.";
            return RedirectToAction(nameof(Details), new { id = poam.Id });
        }

        await PopulateDropdownsAsync(model.SystemId, model.POCId);
        return View(model);
    }

    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> Delete(int id)
    {
        var poam = await _context.POAMs
            .Include(p => p.System)
            .Include(p => p.Milestones)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (poam == null)
            return NotFound();

        return View(poam);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var poam = await _context.POAMs.FindAsync(id);
        if (poam == null)
            return NotFound();

        var identifier = poam.ItemIdentifier;

        _context.POAMs.Remove(poam);
        await _context.SaveChangesAsync();

        var currentUser = await GetCurrentUserAsync();
        await _auditService.LogAsync(currentUser?.Id, "Delete", "POAM", id, poam, null, GetIpAddress());

        TempData["Success"] = $"POAM {identifier} deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? selectedSystemId = null, int? selectedPOCId = null)
    {
        ViewBag.Systems = new SelectList(
            await _context.Systems.OrderBy(s => s.SystemName).ToListAsync(),
            "Id", "SystemName", selectedSystemId);

        ViewBag.Users = new SelectList(
            await _context.Users.Where(u => u.IsActive).OrderBy(u => u.DisplayName).ToListAsync(),
            "Id", "DisplayName", selectedPOCId);
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
