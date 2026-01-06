using POAMs.Web.Models.Domain;

namespace POAMs.Web.Models.ViewModels;

public class DashboardViewModel
{
    public int TotalPOAMs { get; set; }
    public int OpenPOAMs { get; set; }
    public int OverduePOAMs { get; set; }
    public int UpcomingDueDates { get; set; }
    public int HighRiskPOAMs { get; set; }

    public List<POAM> RecentPOAMs { get; set; } = new();
    public List<Milestone> UpcomingMilestones { get; set; } = new();
    public List<StatusCount> POAMsByStatus { get; set; } = new();
    public List<RiskCount> POAMsByRisk { get; set; } = new();
}

public class StatusCount
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RiskCount
{
    public string RiskLevel { get; set; } = string.Empty;
    public int Count { get; set; }
}
