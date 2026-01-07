using POAMs.Web.Models.ActiveDirectory;
using POAMs.Web.Models.Domain;

namespace POAMs.Web.Services;

public interface IActiveDirectoryService
{
    Task<ADConnectionTestResult> TestConnectionAsync();

    Task<ADUserInfo?> GetUserByWindowsIdentityAsync(string windowsIdentity);

    Task<ADUserInfo?> GetUserBySamAccountNameAsync(string samAccountName);

    Task<IEnumerable<ADUserInfo>> SearchUsersAsync(string searchTerm, int maxResults = 25);

    Task<bool> IsAvailableAsync();

    Task<ADConfiguration?> GetConfigurationAsync();

    string EncryptPassword(string plainText);

    string DecryptPassword(string encryptedText);
}
