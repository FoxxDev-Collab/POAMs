namespace POAMs.Web.Models.Domain;

public enum UserRole
{
    Admin,
    ISSM,
    ISSO,
    SysAdmin,
    Auditor
}

public enum RiskLevel
{
    Low,
    Moderate,
    High
}

public enum POAMStatus
{
    Draft,
    Open,
    Ongoing,
    Completed,
    Closed
}

public enum MilestoneStatus
{
    NotStarted,
    InProgress,
    Completed,
    Blocked
}

public enum AssignmentType
{
    POC,
    Contributor,
    Reviewer
}

public enum ControlAssessmentStatus
{
    NotAssessed,
    Compliant,
    NonCompliant,
    NotApplicable,
    Inherited
}

/// <summary>
/// Type of documentation artifact required for a control
/// </summary>
public enum DocumentationType
{
    Policy,
    Procedure,
    Plan,
    Report,
    Record,
    Template,
    Evidence
}

/// <summary>
/// Priority level for documentation requirements
/// </summary>
public enum DocumentationPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Status of documentation artifact
/// </summary>
public enum DocumentationStatus
{
    NotStarted,
    InProgress,
    Draft,
    UnderReview,
    Approved,
    Complete
}

/// <summary>
/// Review status for documentation
/// </summary>
public enum ReviewStatus
{
    Pending,
    InReview,
    ChangesRequested,
    Approved
}
