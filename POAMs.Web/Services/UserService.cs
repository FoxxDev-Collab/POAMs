using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
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

    public async Task<User?> GetOrCreateWindowsUserAsync(string windowsUsername, string? email = null)
    {
        var user = await GetByUsernameAsync(windowsUsername);
        if (user != null)
            return user;

        // Create new user from Windows auth
        user = new User
        {
            Username = windowsUsername,
            Email = email ?? $"{windowsUsername.Replace("\\", ".")}@domain.local",
            DisplayName = windowsUsername.Contains('\\')
                ? windowsUsername.Split('\\').Last()
                : windowsUsername,
            Role = UserRole.ISSO, // Default role for new Windows users
            IsWindowsAuth = true,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
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
