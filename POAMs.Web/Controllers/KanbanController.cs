using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using POAMs.Web.Services;

namespace POAMs.Web.Controllers;

[Authorize(Policy = "CanView")]
public class KanbanController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IUserService _userService;

    public KanbanController(ApplicationDbContext context, IAuditService auditService, IUserService userService)
    {
        _context = context;
        _auditService = auditService;
        _userService = userService;
    }

    public async Task<IActionResult> Index(int? systemId)
    {
        var systems = await _context.Systems.OrderBy(s => s.SystemName).ToListAsync();
        ViewBag.Systems = systems;
        ViewBag.SelectedSystemId = systemId;

        var query = _context.POAMs
            .Include(p => p.System)
            .Include(p => p.POC)
            .Include(p => p.Milestones)
            .AsQueryable();

        if (systemId.HasValue)
        {
            query = query.Where(p => p.SystemId == systemId.Value);
        }

        var poams = await query.OrderBy(p => p.ScheduledCompletionDate).ToListAsync();

        var board = new Dictionary<POAMStatus, List<POAM>>
        {
            { POAMStatus.Draft, poams.Where(p => p.Status == POAMStatus.Draft).ToList() },
            { POAMStatus.Open, poams.Where(p => p.Status == POAMStatus.Open).ToList() },
            { POAMStatus.Ongoing, poams.Where(p => p.Status == POAMStatus.Ongoing).ToList() },
            { POAMStatus.Completed, poams.Where(p => p.Status == POAMStatus.Completed).ToList() },
            { POAMStatus.Closed, poams.Where(p => p.Status == POAMStatus.Closed).ToList() }
        };

        return View(board);
    }

    [HttpPost]
    [Authorize(Policy = "CanEdit")]
    public async Task<IActionResult> UpdateStatus(int id, POAMStatus status)
    {
        var poam = await _context.POAMs.FindAsync(id);
        if (poam == null)
            return NotFound();

        var oldStatus = poam.Status;
        poam.Status = status;
        poam.LastUpdateDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var currentUser = await GetCurrentUserAsync();
        await _auditService.LogAsync(
            currentUser?.Id,
            "UpdateStatus",
            "POAM",
            poam.Id,
            new { Status = oldStatus.ToString() },
            new { Status = status.ToString() },
            GetIpAddress());

        return Json(new { success = true });
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
