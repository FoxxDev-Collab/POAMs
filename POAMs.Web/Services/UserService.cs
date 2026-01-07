using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.ActiveDirectory;
using POAMs.Web.Models.Domain;
using System.Security.Cryptography;

namespace POAMs.Web.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public UserService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() && u.IsActive);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByADGuidAsync(string objectGuid)
    {
        if (string.IsNullOrEmpty(objectGuid))
            return null;
        return await _context.Users
            .FirstOrDefaultAsync(u => u.ADObjectGuid == objectGuid && u.IsActive);
    }

    public async Task<User?> GetOrCreateWindowsUserAsync(string windowsUsername, ADUserInfo? adInfo = null)
    {
        // First try to find by username
        var user = await GetByUsernameAsync(windowsUsername);
        if (user != null)
        {
            // Update AD sync info if we have it
            if (adInfo != null)
            {
                user.LastADSync = DateTime.UtcNow;
                user.ADDisplayName = adInfo.DisplayName;
                user.ADEmail = adInfo.Email;
                if (string.IsNullOrEmpty(user.ADObjectGuid))
                    user.ADObjectGuid = adInfo.ObjectGuid;
                await _context.SaveChangesAsync();
            }
            return user;
        }

        // If AD info provided, also try to find by ObjectGuid (in case username changed)
        if (adInfo != null && !string.IsNullOrEmpty(adInfo.ObjectGuid))
        {
            user = await GetByADGuidAsync(adInfo.ObjectGuid);
            if (user != null)
            {
                // Update username if it changed in AD
                user.Username = windowsUsername;
                user.LastADSync = DateTime.UtcNow;
                user.ADDisplayName = adInfo.DisplayName;
                user.ADEmail = adInfo.Email;
                await _context.SaveChangesAsync();
                return user;
            }
        }

        // Get default role from AD config
        var adConfig = await _context.ADConfigurations.FirstOrDefaultAsync();
        var defaultRole = adConfig?.DefaultNewUserRole ?? UserRole.ISSO;

        // Create new user from Windows auth
        user = new User
        {
            Username = windowsUsername,
            Email = adInfo?.Email ?? $"{windowsUsername.Replace("\\", ".")}@domain.local",
            DisplayName = adInfo?.DisplayName ?? ExtractDisplayName(windowsUsername),
            Phone = adInfo?.Phone,
            Role = defaultRole,
            IsWindowsAuth = true,
            IsActive = true,
            ADObjectGuid = adInfo?.ObjectGuid,
            ADDisplayName = adInfo?.DisplayName,
            ADEmail = adInfo?.Email,
            LastADSync = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private static string ExtractDisplayName(string windowsUsername)
    {
        var parts = windowsUsername.Split('\\');
        return parts.Length > 1 ? parts[1] : parts[0];
    }

    public async Task<User> CreateLocalUserAsync(string username, string email, string password, string displayName, UserRole role)
    {
        ValidatePassword(password);

        var user = new User
        {
            Username = username,
            Email = email,
            DisplayName = displayName,
            PasswordHash = HashPassword(password),
            Role = role,
            IsWindowsAuth = false,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ValidatePasswordAsync(User user, string password)
    {
        if (user.IsWindowsAuth || string.IsNullOrEmpty(user.PasswordHash))
            return false;

        return await Task.FromResult(VerifyPassword(password, user.PasswordHash));
    }

    public async Task UpdatePasswordAsync(User user, string newPassword)
    {
        ValidatePassword(newPassword);
        user.PasswordHash = HashPassword(newPassword);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _context.Users.OrderBy(u => u.DisplayName).ToListAsync();
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task DeactivateUserAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 32);

        byte[] result = new byte[48];
        Buffer.BlockCopy(salt, 0, result, 0, 16);
        Buffer.BlockCopy(hash, 0, result, 16, 32);
        return Convert.ToBase64String(result);
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        byte[] stored = Convert.FromBase64String(storedHash);
        byte[] salt = new byte[16];
        Buffer.BlockCopy(stored, 0, salt, 0, 16);

        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 32);

        for (int i = 0; i < 32; i++)
        {
            if (stored[i + 16] != hash[i])
                return false;
        }
        return true;
    }

    private void ValidatePassword(string password)
    {
        var policy = _configuration.GetSection("PasswordPolicy");
        int minLength = policy.GetValue<int>("MinLength", 12);
        bool requireUppercase = policy.GetValue<bool>("RequireUppercase", true);
        bool requireLowercase = policy.GetValue<bool>("RequireLowercase", true);
        bool requireDigit = policy.GetValue<bool>("RequireDigit", true);
        bool requireNonAlphanumeric = policy.GetValue<bool>("RequireNonAlphanumeric", true);

        var errors = new List<string>();

        if (password.Length < minLength)
            errors.Add($"Password must be at least {minLength} characters long.");

        if (requireUppercase && !password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter.");

        if (requireLowercase && !password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter.");

        if (requireDigit && !password.Any(char.IsDigit))
            errors.Add("Password must contain at least one digit.");

        if (requireNonAlphanumeric && password.All(char.IsLetterOrDigit))
            errors.Add("Password must contain at least one special character.");

        if (errors.Any())
            throw new ArgumentException(string.Join(" ", errors));
    }
}
