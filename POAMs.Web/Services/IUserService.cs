using POAMs.Web.Models.Domain;

namespace POAMs.Web.Services;

public interface IUserService
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetOrCreateWindowsUserAsync(string windowsUsername, string? email = null);
    Task<User> CreateLocalUserAsync(string username, string email, string password, string displayName, UserRole role);
    Task<bool> ValidatePasswordAsync(User user, string password);
    Task UpdatePasswordAsync(User user, string newPassword);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User> UpdateUserAsync(User user);
    Task DeactivateUserAsync(int userId);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
