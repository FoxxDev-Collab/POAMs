using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;

namespace POAMs.Web.Controllers;

[Authorize(Policy = "CanView")]
public class CalendarController : Controller
{
    private readonly ApplicationDbContext _context;

    public CalendarController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents(DateTime start, DateTime end)
    {
        var events = new List<object>();

        // Get POAM scheduled completion dates
        var poams = await _context.POAMs
            .Include(p => p.System)
            .Where(p => p.ScheduledCompletionDate >= start && p.ScheduledCompletionDate <= end)
            .ToListAsync();

        foreach (var poam in poams)
        {
            var color = poam.Status switch
            {
                Models.Domain.POAMStatus.Completed => "#198754", // green
                Models.Domain.POAMStatus.Closed => "#6c757d", // gray
                Models.Domain.POAMStatus.Draft => "#ffc107", // yellow
                _ => poam.RiskLevel switch
                {
                    Models.Domain.RiskLevel.High => "#dc3545", // red
                    Models.Domain.RiskLevel.Moderate => "#fd7e14", // orange
                    _ => "#0d6efd" // blue
                }
            };

            events.Add(new
            {
                id = $"poam-{poam.Id}",
                title = $"POAM: {poam.ItemIdentifier}",
                start = poam.ScheduledCompletionDate.ToString("yyyy-MM-dd"),
                url = Url.Action("Details", "POAM", new { id = poam.Id }),
                backgroundColor = color,
                borderColor = color,
                extendedProps = new
                {
                    type = "poam",
                    status = poam.Status.ToString(),
                    risk = poam.RiskLevel.ToString(),
                    system = poam.System?.SystemName
                }
            });
        }

        // Get Milestone due dates
        var milestones = await _context.Milestones
            .Include(m => m.POAM)
            .Where(m => m.DueDate >= start && m.DueDate <= end)
            .ToListAsync();

        foreach (var milestone in milestones)
        {
            var color = milestone.Status switch
            {
                Models.Domain.MilestoneStatus.Completed => "#198754", // green
                Models.Domain.MilestoneStatus.Blocked => "#dc3545", // red
                Models.Domain.MilestoneStatus.InProgress => "#0dcaf0", // cyan
                _ => "#6c757d" // gray
            };

            events.Add(new
            {
                id = $"milestone-{milestone.Id}",
                title = $"MS {milestone.MilestoneNumber}: {milestone.Title}",
                start = milestone.DueDate.ToString("yyyy-MM-dd"),
                url = Url.Action("Details", "POAM", new { id = milestone.POAMId }),
                backgroundColor = color,
                borderColor = color,
                extendedProps = new
                {
                    type = "milestone",
                    status = milestone.Status.ToString(),
                    poamId = milestone.POAM?.ItemIdentifier
                }
            });
        }

        return Json(events);
    }
}
