namespace POAMs.Web.Models.Domain;

public class STPTestCase
{
    public int Id { get; set; }

    // Parent STP
    public int SecurityTestPlanId { get; set; }
    public SecurityTestPlan? SecurityTestPlan { get; set; }

    // STIG Finding Information
    public string VulnId { get; set; } = string.Empty; // V-XXXXXX
    public string RuleId { get; set; } = string.Empty; // SV-XXXXXX_rule
    public STIGSeverity Severity { get; set; } = STIGSeverity.CAT_II;
    public string RuleVersion { get; set; } = string.Empty; // XXXX-XX-XXXXXX
    public string Title { get; set; } = string.Empty; // STIG Rule Title

    // STIG Check Content and Fix
    public string CheckContent { get; set; } = string.Empty; // Verification steps from STIG
    public string FixText { get; set; } = string.Empty; // Remediation steps from STIG

    // Test Details
    public string ExpectedResult { get; set; } = "Compliant per STIG check";
    public string ActualResult { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty; // Evidence/screenshots
    public string Comments { get; set; } = string.Empty; // Additional notes

    // Status
    public TestCaseStatus Status { get; set; } = TestCaseStatus.NotTested;

    // Tester Information
    public int? TesterId { get; set; }
    public User? Tester { get; set; }
    public DateTime? TestDate { get; set; }
    public bool RetestRequired { get; set; } = false;

    // Order for display
    public int SortOrder { get; set; }

    // Audit fields
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}

public enum STIGSeverity
{
    CAT_I,   // High
    CAT_II,  // Medium
    CAT_III  // Low
}

public enum TestCaseStatus
{
    NotTested,
    Pass,
    Fail,
    NotApplicable
}
