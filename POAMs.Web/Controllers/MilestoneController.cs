using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;
using POAMs.Web.Services;

namespace POAMs.Web.Controllers;

[Authorize(Policy = "CanEdit")]
public class MilestoneController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    public MilestoneController(ApplicationDbContext context, IUserService userService, IAuditService auditService)
    {
        _context = context;
        _userService = userService;
        _auditService = auditService;
    }

    public async Task<IActionResult> Create(int poamId)
    {
        var poam = await _context.POAMs.FindAsync(poamId);
        if (poam == null)
            return NotFound();

        var nextNumber = await _context.Milestones
            .Where(m => m.POAMId == poamId)
            .MaxAsync(m => (int?)m.MilestoneNumber) ?? 0;

        var model = new MilestoneEditViewModel
        {
            POAMId = poamId,
            POAMIdentifier = poam.ItemIdentifier,
            MilestoneNumber = nextNumber + 1,
            DueDate = DateTime.UtcNow.AddDays(30),
            Status = MilestoneStatus.NotStarted
        };

        await PopulateDropdownsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MilestoneEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var milestone = new Milestone
            {
                POAMId = model.POAMId,
                MilestoneNumber = model.MilestoneNumber,
                Title = model.Title,
                DueDate = model.DueDate,
                Status = model.Status,
                Changes = model.Changes,
                AssignedToId = model.AssignedToId
            };

            _context.Milestones.Add(milestone);
            await _context.SaveChangesAsync();

            var currentUser = await GetCurrentUserAsync();
            await _auditService.LogAsync(currentUser?.Id, "Create", "Milestone", milestone.Id, null, milestone, GetIpAddress());

            TempData["Success"] = $"Milestone {milestone.MilestoneNumber} created successfully.";
            return RedirectToAction("Details", "POAM", new { id = model.POAMId });
        }

        var poam = await _context.POAMs.FindAsync(model.POAMId);
        model.POAMIdentifier = poam?.ItemIdentifier ?? "";
        await PopulateDropdownsAsync(model.AssignedToId);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var milestone = await _context.Milestones
            .Include(m => m.POAM)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (milestone == null)
            return NotFound();

        var model = new MilestoneEditViewModel
        {
            Id = milestone.Id,
            POAMId = milestone.POAMId,
            POAMIdentifier = milestone.POAM.ItemIdentifier,
            MilestoneNumber = milestone.MilestoneNumber,
            Title = milestone.Title,
            DueDate = milestone.DueDate,
            CompletedDate = milestone.CompletedDate,
            Status = milestone.Status,
            Changes = milestone.Changes,
            AssignedToId = milestone.AssignedToId
        };

        await PopulateDropdownsAsync(milestone.AssignedToId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MilestoneEditViewModel model)
    {
        if (id != model.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            var milestone = await _context.Milestones.FindAsync(id);
            if (milestone == null)
                return NotFound();

            var oldValues = new
            {
                milestone.MilestoneNumber,
                milestone.Title,
                milestone.DueDate,
                milestone.CompletedDate,
                milestone.Status,
                milestone.Changes,
                milestone.AssignedToId
            };

            milestone.MilestoneNumber = model.MilestoneNumber;
            milestone.Title = model.Title;
            milestone.DueDate = model.DueDate;
            milestone.Status = model.Status;
            milestone.Changes = model.Changes;
            milestone.AssignedToId = model.AssignedToId;

            // Auto-set completed date
            if (model.Status == MilestoneStatus.Completed && milestone.CompletedDate == null)
            {
                milestone.CompletedDate = DateTime.UtcNow;
            }
            else if (model.Status != MilestoneStatus.Completed)
            {
                milestone.CompletedDate = null;
            }

            await _context.SaveChangesAsync();

            var currentUser = await GetCurrentUserAsync();
            await _auditService.LogAsync(currentUser?.Id, "Update", "Milestone", milestone.Id, oldValues, model, GetIpAddress());

            TempData["Success"] = $"Milestone {milestone.MilestoneNumber} updated successfully.";
            return RedirectToAction("Details", "POAM", new { id = milestone.POAMId });
        }

        var poam = await _context.POAMs.FindAsync(model.POAMId);
        model.POAMIdentifier = poam?.ItemIdentifier ?? "";
        await PopulateDropdownsAsync(model.AssignedToId);
        return View(model);
    }

    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> Delete(int id)
    {
        var milestone = await _context.Milestones
            .Include(m => m.POAM)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (milestone == null)
            return NotFound();

        return View(milestone);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "ManagerOrAbove")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var milestone = await _context.Milestones.FindAsync(id);
        if (milestone == null)
            return NotFound();

        var poamId = milestone.POAMId;
        var number = milestone.MilestoneNumber;

        _context.Milestones.Remove(milestone);
        await _context.SaveChangesAsync();

        var currentUser = await GetCurrentUserAsync();
        await _auditService.LogAsync(currentUser?.Id, "Delete", "Milestone", id, milestone, null, GetIpAddress());

        TempData["Success"] = $"Milestone {number} deleted successfully.";
        return RedirectToAction("Details", "POAM", new { id = poamId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, MilestoneStatus status)
    {
        var milestone = await _context.Milestones.FindAsync(id);
        if (milestone == null)
            return NotFound();

        var oldStatus = milestone.Status;
        milestone.Status = status;

        if (status == MilestoneStatus.Completed && milestone.CompletedDate == null)
        {
            milestone.CompletedDate = DateTime.UtcNow;
        }
        else if (status != MilestoneStatus.Completed)
        {
            milestone.CompletedDate = null;
        }

        await _context.SaveChangesAsync();

        var currentUser = await GetCurrentUserAsync();
        await _auditService.LogAsync(currentUser?.Id, "UpdateStatus", "Milestone", id,
            new { Status = oldStatus }, new { Status = status }, GetIpAddress());

        TempData["Success"] = $"Milestone status updated to {status}.";
        return RedirectToAction("Details", "POAM", new { id = milestone.POAMId });
    }

    private async Task PopulateDropdownsAsync(int? selectedUserId = null)
    {
        ViewBag.Users = new SelectList(
            await _context.Users.Where(u => u.IsActive).OrderBy(u => u.DisplayName).ToListAsync(),
            "Id", "DisplayName", selectedUserId);
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
