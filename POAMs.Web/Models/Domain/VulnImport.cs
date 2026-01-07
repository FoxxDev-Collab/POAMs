using System.ComponentModel.DataAnnotations;

namespace POAMs.Web.Models.Domain;

/// <summary>
/// Represents an import session from the Vulnerability Management application.
/// Each import creates a new session to allow historical comparison.
/// </summary>
public class VulnImportSession
{
    public int Id { get; set; }

    public DateTime ImportDate { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string ImportedBy { get; set; } = string.Empty;

    [MaxLength(200)]
    public string SourceApplication { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ExportType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    // Summary stats (denormalized for quick dashboard loading)
    public int TotalSites { get; set; }
    public int TotalHosts { get; set; }
    public int TotalStigChecklists { get; set; }
    public int TotalStigFindings { get; set; }
    public int StigOpen { get; set; }
    public int StigNotAFinding { get; set; }
    public int StigNotApplicable { get; set; }
    public int StigNotReviewed { get; set; }
    public int TotalNessusVulns { get; set; }
    public int NessusCritical { get; set; }
    public int NessusHigh { get; set; }
    public int NessusMedium { get; set; }
    public int NessusLow { get; set; }
    public int NessusInfo { get; set; }
    public int UniqueCCIs { get; set; }
    public int CCIsWithOpenFindings { get; set; }

    // Computed properties for dashboard
    public decimal StigCompliancePercentage => TotalStigFindings > 0
        ? Math.Round((decimal)StigNotAFinding / TotalStigFindings * 100, 1)
        : 0;

    public string RiskLevel
    {
        get
        {
            if (NessusCritical > 0 || StigOpen > 10) return "Critical";
            if (NessusHigh > 0 || StigOpen > 5) return "High";
            if (NessusMedium > 5 || StigOpen > 0) return "Medium";
            if (NessusLow > 0) return "Low";
            return "Minimal";
        }
    }

    // Navigation
    public ICollection<VulnImportHost> Hosts { get; set; } = new List<VulnImportHost>();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a scanned host from the import.
/// </summary>
public class VulnImportHost
{
    public int Id { get; set; }

    public int VulnImportSessionId { get; set; }
    public VulnImportSession Session { get; set; } = null!;

    [MaxLength(200)]
    public string SiteName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string DnsName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(200)]
    public string OperatingSystem { get; set; } = string.Empty;

    [MaxLength(100)]
    public string AssetType { get; set; } = string.Empty;

    // Navigation
    public ICollection<VulnImportStigResult> StigResults { get; set; } = new List<VulnImportStigResult>();
    public ICollection<VulnImportNessusVuln> NessusVulns { get; set; } = new List<VulnImportNessusVuln>();
}

/// <summary>
/// Represents a STIG finding from the import with CCI mapping capability.
/// </summary>
public class VulnImportStigResult
{
    public int Id { get; set; }

    public int VulnImportHostId { get; set; }
    public VulnImportHost Host { get; set; } = null!;

    [MaxLength(100)]
    public string StigId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string StigTitle { get; set; } = string.Empty;

    [MaxLength(50)]
    public string VulnId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string RuleId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string RuleTitle { get; set; } = string.Empty;

    /// <summary>
    /// Severity level: High, Medium, Low (mapped to CAT I, II, III)
    /// </summary>
    [MaxLength(20)]
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Status: Open, NotAFinding, NotApplicable, NotReviewed
    /// </summary>
    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of CCI strings (e.g., ["CCI-000366", "CCI-001199"])
    /// </summary>
    public string CCIs { get; set; } = "[]";

    /// <summary>
    /// Returns the STIG CAT level based on severity
    /// </summary>
    public string CatLevel => Severity?.ToLower() switch
    {
        "high" => "CAT I",
        "medium" => "CAT II",
        "low" => "CAT III",
        _ => "Unknown"
    };
}

/// <summary>
/// Represents a Nessus vulnerability finding (metrics only, no tracking).
/// </summary>
public class VulnImportNessusVuln
{
    public int Id { get; set; }

    public int VulnImportHostId { get; set; }
    public VulnImportHost Host { get; set; } = null!;

    public int PluginId { get; set; }

    [MaxLength(500)]
    public string PluginName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Family { get; set; } = string.Empty;

    /// <summary>
    /// Severity: Critical, High, Medium, Low, Info
    /// </summary>
    [MaxLength(20)]
    public string Severity { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CVE { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Synopsis { get; set; } = string.Empty;

    public bool IsExploitable { get; set; }
}
