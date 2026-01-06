namespace POAMs.Web.Models.Domain;

/// <summary>
/// Test case for NIST 800-53 Security Control assessment (CA-2)
/// Based on the KS2 Post-System-Implementation Critical Controls Test Plan format
/// </summary>
public class SecurityControlTestCase
{
    public int Id { get; set; }

    // Parent STP
    public int SecurityTestPlanId { get; set; }
    public SecurityTestPlan? SecurityTestPlan { get; set; }

    // Control Information
    public string ControlFamily { get; set; } = string.Empty; // AC, AU, IA, etc.
    public string ControlId { get; set; } = string.Empty; // AC-2, AC-6(10), etc.
    public string ControlTitle { get; set; } = string.Empty; // "ACCESS ENFORCEMENT"
    public string ControlEnhancement { get; set; } = string.Empty; // For control enhancements like (2), (7), etc.

    // Related STIG IDs (if applicable)
    public string RelatedSTIGIds { get; set; } = string.Empty; // e.g., "RHEL-08-010040, RHEL-08-010049"

    // Assessment Objective
    public string AssessmentObjective { get; set; } = string.Empty;
    public string AssessmentObjectiveItems { get; set; } = string.Empty; // JSON array of sub-items like AC-6(10)[1], [2], [3]

    // Inheritance
    public ControlInheritance Inheritance { get; set; } = ControlInheritance.None;
    public string InheritedFrom { get; set; } = string.Empty; // e.g., "AF.100.AFIC CCP"

    // Monitoring Requirements
    public MonitoringRequirement MonitoringType { get; set; } = MonitoringRequirement.None;

    // Assessment Method Flags (EXAMINE, INTERVIEW, TEST)
    public bool AssessExamine { get; set; } = false;
    public bool AssessInterview { get; set; } = false;
    public bool AssessTest { get; set; } = false;

    // Status
    public ControlTestStatus Status { get; set; } = ControlTestStatus.NotAssessed;

    // Test Steps (stored as JSON for flexibility)
    public string TestSteps { get; set; } = string.Empty; // JSON array of test steps

    // Results
    public string OverallResult { get; set; } = string.Empty; // Pass/Fail summary
    public string Comments { get; set; } = string.Empty;

    // Reviewer Information
    public string ReviewerName { get; set; } = string.Empty;
    public DateTime? ReviewDate { get; set; }

    // Order for display
    public int SortOrder { get; set; }

    // Audit fields
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual test step within a Security Control test case
/// </summary>
public class ControlTestStep
{
    public int StepNumber { get; set; }
    public string RequiredAction { get; set; } = string.Empty;
    public string ExpectedResults { get; set; } = string.Empty;
    public string ActualResults { get; set; } = string.Empty;
    public StepResult Result { get; set; } = StepResult.NotTested;
    public string Comments { get; set; } = string.Empty;
}

public enum ControlTestStatus
{
    NotAssessed,
    Deferred,
    NotExecuted,
    Failed,
    NotApplicable,
    Passed
}

public enum ControlInheritance
{
    None,
    AFIC_CCP,           // AF.100.AFIC CCP
    AFIC_SCI_CCP,       // AF.207.AFIC SCI Only CCP
    AFMC_IR_CCP,        // AF.208.AFMC IR CCP
    Other
}

public enum MonitoringRequirement
{
    None,
    Annual,             // * Must be addressed annually
    ContinuousMon1,     // ** Continuous Monitoring 1st year
    ContinuousMon2,     // ** (underlined) Continuous Monitoring 2nd year
    ContinuousMon3      // ** (double underlined) Continuous Monitoring 3rd year
}

public enum StepResult
{
    NotTested,
    StepComplete,
    Pass,
    Fail
}
