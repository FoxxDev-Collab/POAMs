namespace POAMs.Web.Models.Domain;

public class SecurityTestPlan
{
    public int Id { get; set; }

    // STP Type - determines which test case collection is used
    public STPType Type { get; set; } = STPType.STIG;

    // Link to POAM (optional - STP can be standalone or linked to POA&M)
    public int? POAMId { get; set; }
    public POAM? POAM { get; set; }

    // Link to System
    public int SystemId { get; set; }
    public SystemInfo? System { get; set; }

    // Test Plan Identification
    public string STPIdentifier { get; set; } = string.Empty; // e.g., "STP-2025-001"
    public string TestPlanVersion { get; set; } = "1.0";
    public DateTime TestPlanDate { get; set; } = DateTime.UtcNow;

    // === STIG-specific fields ===
    public string STIGName { get; set; } = string.Empty; // e.g., "Red Hat Enterprise Linux 8 STIG"
    public string STIGVersion { get; set; } = string.Empty; // e.g., "V1R14"
    public DateTime? STIGReleaseDate { get; set; }

    // === Nessus-specific fields ===
    public string ScanPolicyName { get; set; } = string.Empty; // e.g., "Advanced Network Scan"
    public DateTime? ScanDate { get; set; }
    public string ScannerVersion { get; set; } = string.Empty; // e.g., "Tenable.sc 6.2"

    // Host/Asset Information (common)
    public string HostName { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public string OSPlatform { get; set; } = string.Empty; // e.g., "RHEL 8.9"
    public string Environment { get; set; } = string.Empty; // Dev/Test/Prod
    public string Classification { get; set; } = "Unclassified"; // Unclassified/CUI/Secret
    public string MACCAL { get; set; } = string.Empty; // e.g., "MAC II / CAL II"

    // Personnel
    public int? LeadAssessorId { get; set; }
    public User? LeadAssessor { get; set; }
    public string Assessor2Name { get; set; } = string.Empty; // For Security Controls STP
    public string Assessor2Position { get; set; } = string.Empty;
    public string AssessmentTeam { get; set; } = string.Empty;
    public int? ISSMId { get; set; }
    public User? ISSM { get; set; }
    public string SystemOwner { get; set; } = string.Empty;
    public string AuthorizingOfficial { get; set; } = string.Empty;

    // Security Controls STP specific
    public string PrivilegedUser { get; set; } = string.Empty;
    public string AuthorizedUser { get; set; } = string.Empty;

    // Testing Dates
    public DateTime? TestStartDate { get; set; }
    public DateTime? TestEndDate { get; set; }

    // Status
    public STPStatus Status { get; set; } = STPStatus.Draft;

    // Executive Summary
    public string ExecutiveSummary { get; set; } = string.Empty;

    // Navigation - STIG Test Cases
    public List<STPTestCase> TestCases { get; set; } = new();

    // Navigation - Security Control Test Cases
    public List<SecurityControlTestCase> ControlTestCases { get; set; } = new();

    // Navigation - Nessus/Vulnerability Test Cases
    public List<NessusTestCase> NessusTestCases { get; set; } = new();

    // Audit fields
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    // Computed properties for STIG summary
    public int TotalTestCases => TestCases.Count;
    public int PassCount => TestCases.Count(t => t.Status == TestCaseStatus.Pass);
    public int FailCount => TestCases.Count(t => t.Status == TestCaseStatus.Fail);
    public int NotApplicableCount => TestCases.Count(t => t.Status == TestCaseStatus.NotApplicable);
    public int NotTestedCount => TestCases.Count(t => t.Status == TestCaseStatus.NotTested);
    public decimal ComplianceRate => TotalTestCases > 0
        ? Math.Round((decimal)(PassCount + NotApplicableCount) / TotalTestCases * 100, 1)
        : 0;

    // By STIG severity counts
    public int CatITotal => TestCases.Count(t => t.Severity == STIGSeverity.CAT_I);
    public int CatIFail => TestCases.Count(t => t.Severity == STIGSeverity.CAT_I && t.Status == TestCaseStatus.Fail);
    public int CatIITotal => TestCases.Count(t => t.Severity == STIGSeverity.CAT_II);
    public int CatIIFail => TestCases.Count(t => t.Severity == STIGSeverity.CAT_II && t.Status == TestCaseStatus.Fail);
    public int CatIIITotal => TestCases.Count(t => t.Severity == STIGSeverity.CAT_III);
    public int CatIIIFail => TestCases.Count(t => t.Severity == STIGSeverity.CAT_III && t.Status == TestCaseStatus.Fail);

    // Computed properties for Security Controls summary
    public int TotalControls => ControlTestCases.Count;
    public int ControlsAssessed => ControlTestCases.Count(c => c.Status != ControlTestStatus.NotAssessed);
    public int ControlsPassed => ControlTestCases.Count(c => c.Status == ControlTestStatus.Passed);
    public int ControlsFailed => ControlTestCases.Count(c => c.Status == ControlTestStatus.Failed);
    public decimal ControlsAssessedPercent => TotalControls > 0
        ? Math.Round((decimal)ControlsAssessed / TotalControls * 100, 1) : 0;
    public decimal ControlsCompliantPercent => ControlsAssessed > 0
        ? Math.Round((decimal)ControlsPassed / ControlsAssessed * 100, 1) : 0;

    // Computed properties for Nessus summary
    public int TotalVulnerabilities => NessusTestCases.Count;
    public int CriticalCount => NessusTestCases.Count(n => n.Severity == NessusSeverity.Critical);
    public int HighCount => NessusTestCases.Count(n => n.Severity == NessusSeverity.High);
    public int MediumCount => NessusTestCases.Count(n => n.Severity == NessusSeverity.Medium);
    public int LowCount => NessusTestCases.Count(n => n.Severity == NessusSeverity.Low);
    public int VulnsRemediated => NessusTestCases.Count(n => n.Status == VulnTestStatus.Remediated);
}

public enum STPStatus
{
    Draft,
    InProgress,
    PendingReview,
    Approved,
    Completed,
    Archived
}

public enum STPType
{
    STIG,           // DISA STIG findings (V-XXXXXX)
    Nessus,         // Tenable Nessus vulnerabilities (Plugin ID, CVE)
    SecurityControl // NIST 800-53 control assessment (CA-2)
}
