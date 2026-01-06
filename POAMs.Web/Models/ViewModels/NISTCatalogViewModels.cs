using System.ComponentModel.DataAnnotations;
using POAMs.Web.Models.Domain;

namespace POAMs.Web.Models.ViewModels;

/// <summary>
/// Summary data for a control family (displayed on catalog index)
/// </summary>
public class FamilySummaryViewModel
{
    public string Family { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public int TotalControls { get; set; }
    public int TotalCCIs { get; set; }
    public int Compliant { get; set; }
    public int NonCompliant { get; set; }
    public int NotApplicable { get; set; }
    public int Inherited { get; set; }
    public int NotAssessed { get; set; }

    public int AssessedCount => Compliant + NonCompliant + NotApplicable + Inherited;

    /// <summary>
    /// Compliance rate = (Compliant + N/A + Inherited) / Total Controls
    /// This gives a realistic picture - unassessed controls count against you
    /// </summary>
    public decimal ComplianceRate => TotalControls > 0
        ? Math.Round((decimal)(Compliant + NotApplicable + Inherited) / TotalControls * 100, 1)
        : 0;

    /// <summary>
    /// Progress rate = Assessed / Total Controls
    /// </summary>
    public decimal ProgressRate => TotalControls > 0
        ? Math.Round((decimal)AssessedCount / TotalControls * 100, 1)
        : 0;
}

/// <summary>
/// Control item for family list view
/// </summary>
public class ControlListItemViewModel
{
    public int Id { get; set; }
    public string ControlId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public int CCICount { get; set; }
    public bool IsWithdrawn { get; set; }
    public bool IsEnhancement { get; set; }
    public string? ParentControlId { get; set; }

    // Assessment status for selected system (null if no system selected)
    public ControlAssessmentStatus? Status { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public int? POAMId { get; set; }
}

/// <summary>
/// Detailed view of a single control
/// </summary>
public class NISTControlDetailViewModel
{
    public int Id { get; set; }
    public string ControlId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string ControlText { get; set; } = string.Empty;
    public string? Discussion { get; set; }
    public List<string> RelatedControls { get; set; } = new();
    public bool IsWithdrawn { get; set; }
    public string? ParentControlId { get; set; }

    public List<CCIViewModel> CCIs { get; set; } = new();

    /// <summary>
    /// Assessment history grouped by system
    /// </summary>
    public List<SystemAssessmentHistoryViewModel> AssessmentsBySystem { get; set; } = new();

    /// <summary>
    /// Documentation requirements and their status for selected system
    /// </summary>
    public List<DocumentationInstanceViewModel> Documentation { get; set; } = new();

    /// <summary>
    /// Whether this control has documentation requirements
    /// </summary>
    public bool HasDocumentationRequirements => Documentation.Any();

    /// <summary>
    /// Count of documentation requirements
    /// </summary>
    public int DocumentationCount => Documentation.Count;

    /// <summary>
    /// Count of completed documentation
    /// </summary>
    public int DocumentationComplete => Documentation.Count(d =>
        d.Status == POAMs.Web.Models.Domain.DocumentationStatus.Complete ||
        d.Status == POAMs.Web.Models.Domain.DocumentationStatus.Approved);
}

/// <summary>
/// CCI for display
/// </summary>
public class CCIViewModel
{
    public int Id { get; set; }
    public string CCINumber { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
}

/// <summary>
/// Assessment summary for control detail view
/// </summary>
public class AssessmentSummaryViewModel
{
    public int Id { get; set; }
    public int SystemId { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public ControlAssessmentStatus Status { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public string? AssessedByName { get; set; }
    public DateTime? AssessedDate { get; set; }
    public int? POAMId { get; set; }
    public string? POAMIdentifier { get; set; }
    public bool IsLatest { get; set; }
}

/// <summary>
/// Groups assessments by system for history display
/// </summary>
public class SystemAssessmentHistoryViewModel
{
    public int SystemId { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public List<AssessmentSummaryViewModel> Assessments { get; set; } = new();

    /// <summary>
    /// The latest assessment determines current status
    /// </summary>
    public AssessmentSummaryViewModel? LatestAssessment => Assessments.FirstOrDefault();
}

/// <summary>
/// ViewModel for creating/editing an assessment
/// </summary>
public class NISTAssessmentEditViewModel
{
    public int? Id { get; set; }

    [Required]
    public string ControlId { get; set; } = string.Empty;

    public string ControlName { get; set; } = string.Empty;

    [Required]
    public int SystemId { get; set; }

    public string SystemName { get; set; } = string.Empty;

    [Required]
    public ControlAssessmentStatus Status { get; set; }

    public RiskLevel? RiskLevel { get; set; }

    [MaxLength(4000)]
    [Display(Name = "Implementation Description")]
    public string? Implementation { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Evidence/Artifacts")]
    public string? Evidence { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Display(Name = "Link to POAM")]
    public int? POAMId { get; set; }
}

/// <summary>
/// Index page view model
/// </summary>
public class NISTCatalogIndexViewModel
{
    public int? SelectedSystemId { get; set; }
    public string? SelectedSystemName { get; set; }
    public List<FamilySummaryViewModel> Families { get; set; } = new();

    // Overall statistics
    public int TotalControls { get; set; }
    public int TotalCCIs { get; set; }
    public int TotalCompliant { get; set; }
    public int TotalNonCompliant { get; set; }
    public int TotalNotAssessed { get; set; }
    public decimal OverallComplianceRate { get; set; }
    public decimal OverallProgressRate { get; set; }
}

/// <summary>
/// Family page view model
/// </summary>
public class NISTFamilyViewModel
{
    public string Family { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public int? SelectedSystemId { get; set; }
    public string? SelectedSystemName { get; set; }
    public string? SearchTerm { get; set; }
    public ControlAssessmentStatus? StatusFilter { get; set; }

    public List<ControlListItemViewModel> Controls { get; set; } = new();
    public FamilySummaryViewModel Summary { get; set; } = new();
}

/// <summary>
/// Helper class to get family full names
/// </summary>
public static class NISTFamilyNames
{
    public static readonly Dictionary<string, string> Names = new()
    {
        { "AC", "Access Control" },
        { "AT", "Awareness and Training" },
        { "AU", "Audit and Accountability" },
        { "CA", "Assessment, Authorization, and Monitoring" },
        { "CM", "Configuration Management" },
        { "CP", "Contingency Planning" },
        { "IA", "Identification and Authentication" },
        { "IR", "Incident Response" },
        { "MA", "Maintenance" },
        { "MP", "Media Protection" },
        { "PE", "Physical and Environmental Protection" },
        { "PL", "Planning" },
        { "PM", "Program Management" },
        { "PS", "Personnel Security" },
        { "PT", "PII Processing and Transparency" },
        { "RA", "Risk Assessment" },
        { "SA", "System and Services Acquisition" },
        { "SC", "System and Communications Protection" },
        { "SI", "System and Information Integrity" },
        { "SR", "Supply Chain Risk Management" }
    };

    public static string GetName(string family) =>
        Names.TryGetValue(family.ToUpper(), out var name) ? name : family;
}
