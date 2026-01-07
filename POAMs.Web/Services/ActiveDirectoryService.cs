using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.ActiveDirectory;
using POAMs.Web.Models.Domain;

namespace POAMs.Web.Services;

public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActiveDirectoryService> _logger;
    private readonly IDataProtector _protector;

    public ActiveDirectoryService(
        ApplicationDbContext context,
        ILogger<ActiveDirectoryService> logger,
        IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _logger = logger;
        _protector = dataProtectionProvider.CreateProtector("ADConfiguration.ServiceAccount");
    }

    public async Task<ADConfiguration?> GetConfigurationAsync()
    {
        return await _context.ADConfigurations.FirstOrDefaultAsync();
    }

    public async Task<bool> IsAvailableAsync()
    {
        var config = await GetConfigurationAsync();
        if (config == null || !config.IsEnabled)
            return false;

        try
        {
            var result = await TestConnectionAsync();
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ADConnectionTestResult> TestConnectionAsync()
    {
        var config = await GetConfigurationAsync();
        if (config == null)
        {
            return new ADConnectionTestResult
            {
                Success = false,
                ErrorMessage = "AD configuration not found"
            };
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var connection = CreateLdapConnection(config);
            var password = DecryptPassword(config.ServiceAccountPasswordEncrypted);
            connection.Bind(new NetworkCredential(
                config.ServiceAccountUsername,
                password,
                config.Domain));

            // Test query - search for 1 user to verify connectivity
            var searchRequest = new SearchRequest(
                config.BaseDN,
                "(objectClass=user)",
                SearchScope.Subtree,
                "sAMAccountName");
            searchRequest.SizeLimit = 1;

            var response = (SearchResponse)connection.SendRequest(searchRequest);
            stopwatch.Stop();

            return new ADConnectionTestResult
            {
                Success = true,
                ResponseTime = stopwatch.Elapsed,
                ServerInfo = $"{config.LdapServer}:{config.LdapPort}"
            };
        }
        catch (LdapException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "LDAP connection test failed");
            return new ADConnectionTestResult
            {
                Success = false,
                ErrorMessage = $"LDAP Error: {ex.Message}",
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Connection test failed with unexpected error");
            return new ADConnectionTestResult
            {
                Success = false,
                ErrorMessage = $"Error: {ex.Message}",
                ResponseTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<ADUserInfo?> GetUserByWindowsIdentityAsync(string windowsIdentity)
    {
        // Parse "DOMAIN\username" to extract username
        var parts = windowsIdentity.Split('\\');
        var samAccountName = parts.Length > 1 ? parts[1] : parts[0];
        return await GetUserBySamAccountNameAsync(samAccountName);
    }

    public async Task<ADUserInfo?> GetUserBySamAccountNameAsync(string samAccountName)
    {
        var config = await GetConfigurationAsync();
        if (config == null || !config.IsEnabled)
            return null;

        try
        {
            using var connection = CreateLdapConnection(config);
            var password = DecryptPassword(config.ServiceAccountPasswordEncrypted);
            connection.Bind(new NetworkCredential(
                config.ServiceAccountUsername,
                password,
                config.Domain));

            var searchRequest = new SearchRequest(
                config.BaseDN,
                $"(&(objectClass=user)(sAMAccountName={EscapeLdapFilter(samAccountName)}))",
                SearchScope.Subtree,
                "sAMAccountName", "userPrincipalName", "displayName", "mail",
                "department", "title", "telephoneNumber", "objectGUID", "userAccountControl");

            var response = (SearchResponse)connection.SendRequest(searchRequest);

            if (response.Entries.Count == 0)
                return null;

            var entry = response.Entries[0];
            return MapToADUserInfo(entry);
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "Failed to query AD for user {Username}", samAccountName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error querying AD for user {Username}", samAccountName);
            return null;
        }
    }

    public async Task<IEnumerable<ADUserInfo>> SearchUsersAsync(string searchTerm, int maxResults = 25)
    {
        var results = new List<ADUserInfo>();
        var config = await GetConfigurationAsync();
        if (config == null || !config.IsEnabled)
            return results;

        try
        {
            using var connection = CreateLdapConnection(config);
            var password = DecryptPassword(config.ServiceAccountPasswordEncrypted);
            connection.Bind(new NetworkCredential(
                config.ServiceAccountUsername,
                password,
                config.Domain));

            var escapedTerm = EscapeLdapFilter(searchTerm);
            var searchRequest = new SearchRequest(
                config.BaseDN,
                $"(&(objectClass=user)(|(sAMAccountName=*{escapedTerm}*)(displayName=*{escapedTerm}*)(mail=*{escapedTerm}*)))",
                SearchScope.Subtree,
                "sAMAccountName", "userPrincipalName", "displayName", "mail",
                "department", "title", "telephoneNumber", "objectGUID", "userAccountControl");
            searchRequest.SizeLimit = maxResults;

            var response = (SearchResponse)connection.SendRequest(searchRequest);

            foreach (SearchResultEntry entry in response.Entries)
            {
                var userInfo = MapToADUserInfo(entry);
                if (userInfo != null)
                    results.Add(userInfo);
            }
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "Failed to search AD for users matching '{SearchTerm}'", searchTerm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error searching AD for users matching '{SearchTerm}'", searchTerm);
        }

        return results;
    }

    public string EncryptPassword(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;
        return _protector.Protect(plainText);
    }

    public string DecryptPassword(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;
        try
        {
            return _protector.Unprotect(encryptedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt service account password");
            return string.Empty;
        }
    }

    private LdapConnection CreateLdapConnection(ADConfiguration config)
    {
        var identifier = new LdapDirectoryIdentifier(config.LdapServer, config.LdapPort);
        var connection = new LdapConnection(identifier)
        {
            AuthType = AuthType.Negotiate
        };

        if (config.UseSsl)
        {
            connection.SessionOptions.SecureSocketLayer = true;
        }

        connection.SessionOptions.ProtocolVersion = 3;
        connection.Timeout = TimeSpan.FromSeconds(30);

        return connection;
    }

    private static ADUserInfo? MapToADUserInfo(SearchResultEntry entry)
    {
        try
        {
            var userAccountControl = GetAttributeValue(entry, "userAccountControl");
            var isEnabled = true;
            if (int.TryParse(userAccountControl, out var uac))
            {
                // Check if account is disabled (bit 1 = 0x0002)
                isEnabled = (uac & 0x0002) == 0;
            }

            var objectGuid = "";
            if (entry.Attributes["objectGUID"]?.Count > 0)
            {
                var guidBytes = (byte[])entry.Attributes["objectGUID"][0];
                objectGuid = new Guid(guidBytes).ToString();
            }

            return new ADUserInfo
            {
                SamAccountName = GetAttributeValue(entry, "sAMAccountName"),
                UserPrincipalName = GetAttributeValue(entry, "userPrincipalName"),
                DisplayName = GetAttributeValue(entry, "displayName"),
                Email = GetAttributeValue(entry, "mail"),
                Department = GetAttributeValue(entry, "department"),
                Title = GetAttributeValue(entry, "title"),
                Phone = GetAttributeValue(entry, "telephoneNumber"),
                ObjectGuid = objectGuid,
                IsEnabled = isEnabled
            };
        }
        catch
        {
            return null;
        }
    }

    private static string GetAttributeValue(SearchResultEntry entry, string attributeName)
    {
        if (entry.Attributes[attributeName]?.Count > 0)
            return entry.Attributes[attributeName][0]?.ToString() ?? string.Empty;
        return string.Empty;
    }

    private static string EscapeLdapFilter(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Escape special LDAP filter characters per RFC 4515
        return input
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");
    }
}
