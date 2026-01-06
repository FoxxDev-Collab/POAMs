namespace POAMs.Web.Models.Domain;

/// <summary>
/// Test case for Tenable Nessus vulnerability findings
/// Used for POA&M remediation testing
/// </summary>
public class NessusTestCase
{
    public int Id { get; set; }

    // Parent STP
    public int SecurityTestPlanId { get; set; }
    public SecurityTestPlan? SecurityTestPlan { get; set; }

    // Nessus Plugin Information
    public string PluginId { get; set; } = string.Empty; // e.g., "12345"
    public string PluginName { get; set; } = string.Empty; // e.g., "SSL Certificate Expired"
    public string PluginFamily { get; set; } = string.Empty; // e.g., "General", "Windows", "Ubuntu"

    // CVE Information
    public string CVE { get; set; } = string.Empty; // e.g., "CVE-2024-1234" or comma-separated list
    public string CVSSScore { get; set; } = string.Empty; // e.g., "9.8"
    public string CVSSVector { get; set; } = string.Empty; // e.g., "CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:H"

    // Severity
    public NessusSeverity Severity { get; set; } = NessusSeverity.Medium;

    // Finding Details
    public string Synopsis { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public string PluginOutput { get; set; } = string.Empty; // Original scan output

    // Affected Assets
    public string AffectedHosts { get; set; } = string.Empty; // Comma-separated list or JSON
    public string AffectedPorts { get; set; } = string.Empty; // e.g., "443/tcp, 8443/tcp"

    // Test Details
    public string ExpectedResult { get; set; } = "Vulnerability no longer detected on rescan";
    public string ActualResult { get; set; } = string.Empty;
    public string RemediationSteps { get; set; } = string.Empty; // What was done to fix
    public string Evidence { get; set; } = string.Empty; // Proof of remediation

    // Status
    public VulnTestStatus Status { get; set; } = VulnTestStatus.NotTested;

    // Verification
    public DateTime? OriginalScanDate { get; set; }
    public DateTime? RescanDate { get; set; }
    public bool RescanRequired { get; set; } = true;
    public string RescanResults { get; set; } = string.Empty;

    // Tester Information
    public int? TesterId { get; set; }
    public User? Tester { get; set; }
    public DateTime? TestDate { get; set; }

    // Order for display
    public int SortOrder { get; set; }

    // Audit fields
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}

public enum NessusSeverity
{
    Critical,   // CVSS 9.0-10.0
    High,       // CVSS 7.0-8.9
    Medium,     // CVSS 4.0-6.9
    Low,        // CVSS 0.1-3.9
    Info        // Informational
}

public enum VulnTestStatus
{
    NotTested,
    InProgress,
    Remediated,         // Fixed and verified
    PartiallyRemediated,// Some instances fixed
    NotRemediated,      // Still vulnerable
    FalsePositive,      // Confirmed false positive
    AcceptedRisk        // Risk accepted, documented
}
