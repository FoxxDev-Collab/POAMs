using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models;
using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;

namespace POAMs.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysFromNow = now.AddDays(30);

        var viewModel = new DashboardViewModel
        {
            TotalPOAMs = await _context.POAMs.CountAsync(),
            OpenPOAMs = await _context.POAMs.CountAsync(p => p.Status == POAMStatus.Open || p.Status == POAMStatus.Ongoing),
            OverduePOAMs = await _context.POAMs.CountAsync(p =>
                p.ScheduledCompletionDate < now &&
                p.Status != POAMStatus.Completed &&
                p.Status != POAMStatus.Closed),
            UpcomingDueDates = await _context.POAMs
                .Where(p => p.ScheduledCompletionDate >= now &&
                           p.ScheduledCompletionDate <= thirtyDaysFromNow &&
                           p.Status != POAMStatus.Completed &&
                           p.Status != POAMStatus.Closed)
                .CountAsync(),
            HighRiskPOAMs = await _context.POAMs.CountAsync(p =>
                p.RiskLevel == RiskLevel.High &&
                p.Status != POAMStatus.Completed &&
                p.Status != POAMStatus.Closed),
            RecentPOAMs = await _context.POAMs
                .Include(p => p.System)
                .Include(p => p.POC)
                .OrderByDescending(p => p.ModifiedDate)
                .Take(5)
                .ToListAsync(),
            UpcomingMilestones = await _context.Milestones
                .Include(m => m.POAM)
                .Where(m => m.DueDate >= now &&
                           m.DueDate <= thirtyDaysFromNow &&
                           m.Status != MilestoneStatus.Completed)
                .OrderBy(m => m.DueDate)
                .Take(10)
                .ToListAsync(),
            POAMsByStatus = await _context.POAMs
                .GroupBy(p => p.Status)
                .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync(),
            POAMsByRisk = await _context.POAMs
                .Where(p => p.Status != POAMStatus.Completed && p.Status != POAMStatus.Closed)
                .GroupBy(p => p.RiskLevel)
                .Select(g => new RiskCount { RiskLevel = g.Key.ToString(), Count = g.Count() })
                .ToListAsync()
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
