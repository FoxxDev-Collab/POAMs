namespace POAMs.Web.Models.ActiveDirectory;

public class ADUserInfo
{
    public string SamAccountName { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? Phone { get; set; }
    public string ObjectGuid { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public class ADConnectionTestResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public string? ServerInfo { get; set; }
}
