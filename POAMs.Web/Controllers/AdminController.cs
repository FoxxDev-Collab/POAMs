using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using POAMs.Web.Models.ViewModels;
using POAMs.Web.Services;

namespace POAMs.Web.Controllers;

[Authorize(Policy = "ManagerOrAbove")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly IActiveDirectoryService _adService;

    public AdminController(
        ApplicationDbContext context,
        IUserService userService,
        IAuditService auditService,
        IActiveDirectoryService adService)
    {
        _context = context;
        _userService = userService;
        _auditService = auditService;
        _adService = adService;
    }

    // User Management
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users.OrderBy(u => u.DisplayName).ToListAsync();
        return View(users);
    }

    public IActionResult CreateUser()
    {
        return View(new UserEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(UserEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Check if username already exists
            var existingUser = await _userService.GetByUsernameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View(model);
            }

            try
            {
                if (model.IsWindowsAuth)
                {
                    var user = new User
                    {
                        Username = model.Username,
                        Email = model.Email,
                        DisplayName = model.DisplayName,
                        Phone = model.Phone,
                        Role = model.Role,
                        IsWindowsAuth = true,
                        IsActive = true
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    var currentUser = await GetCurrentUserAsync();
                    await _auditService.LogAsync(currentUser?.Id, "Create", "User", user.Id, null, user, GetIpAddress());
                }
                else
                {
                    if (string.IsNullOrEmpty(model.Password))
                    {
                        ModelState.AddModelError("Password", "Password is required for local accounts.");
                        return View(model);
                    }

                    var user = await _userService.CreateLocalUserAsync(
                        model.Username, model.Email, model.Password, model.DisplayName, model.Role);
                    user.Phone = model.Phone;
                    await _context.SaveChangesAsync();

                    var currentUser = await GetCurrentUserAsync();
                    await _auditService.LogAsync(currentUser?.Id, "Create", "User", user.Id, null, user, GetIpAddress());
                }

                TempData["Success"] = $"User {model.Username} created successfully.";
                return RedirectToAction(nameof(Users));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("Password", ex.Message);
                return View(model);
            }
        }

        return View(model);
    }

    public async Task<IActionResult> EditUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        var model = new UserEditViewModel
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Phone = user.Phone,
            Role = user.Role,
            IsWindowsAuth = user.IsWindowsAuth,
            IsActive = user.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(int id, UserEditViewModel model)
    {
        if (id != model.Id)
            return NotFound();

        // Remove password validation for edits if no new password
        if (string.IsNullOrEmpty(model.Password))
        {
            ModelState.Remove("Password");
        }

        if (ModelState.IsValid)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var oldValues = new { user.Email, user.DisplayName, user.Phone, user.Role, user.IsActive };

            user.Email = model.Email;
            user.DisplayName = model.DisplayName;
            user.Phone = model.Phone;
            user.Role = model.Role;
            user.IsActive = model.IsActive;

            // Update password if provided
            if (!string.IsNullOrEmpty(model.Password) && !user.IsWindowsAuth)
            {
                try
                {
                    await _userService.UpdatePasswordAsync(user, model.Password);
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("Password", ex.Message);
                    return View(model);
                }
            }

            await _context.SaveChangesAsync();

            var currentUser = await GetCurrentUserAsync();
            await _auditService.LogAsync(currentUser?.Id, "Update", "User", user.Id, oldValues, model, GetIpAddress());

            TempData["Success"] = $"User {user.Username} updated successfully.";
            return RedirectToAction(nameof(Users));
        }

        return View(model);
    }

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        return View(user);
    }

    [HttpPost, ActionName("DeleteUser")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUserConfirmed(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        // Don't allow deleting the last admin
        if (user.Role == UserRole.Admin)
        {
            var adminCount = await _context.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive);
            if (adminCount <= 1)
            {
                TempData["Error"] = "Cannot delete the last admin user.";
                return RedirectToAction(nameof(Users));
            }
        }

        var username = user.Username;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        var currentUser = await GetCurrentUserAsync();
        await _auditService.LogAsync(currentUser?.Id, "Delete", "User", id, user, null, GetIpAddress());

        TempData["Success"] = $"User {username} deleted successfully.";
        return RedirectToAction(nameof(Users));
    }

    // System Management
    public async Task<IActionResult> Systems()
    {
        var systems = await _context.Systems
            .Include(s => s.ISSM)
            .OrderBy(s => s.SystemName)
            .ToListAsync();
        return View(systems);
    }

    public async Task<IActionResult> CreateSystem()
    {
        await PopulateUsersDropdownAsync();
        return View(new SystemEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSystem(SystemEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var system = new SystemInfo
            {
                SystemName = model.SystemName,
                OrganizationName = model.OrganizationName,
                ISType = model.ISType,
                UID = model.UID,
                ISSMId = model.ISSMId
            };

            _context.Systems.Add(system);
            await _context.SaveChangesAsync();

            var currentUser = await GetCurrentUserAsync();
            await _auditService.LogAsync(currentUser?.Id, "Create", "System", system.Id, null, system, GetIpAddress());

            TempData["Success"] = $"System {system.SystemName} created successfully.";
            return RedirectToAction(nameof(Systems));
        }

        await PopulateUsersDropdownAsync(model.ISSMId);
        return View(model);
    }

    public async Task<IActionResult> EditSystem(int id)
    {
        var system = await _context.Systems.FindAsync(id);
        if (system == null)
            return NotFound();

        var model = new SystemEditViewModel
        {
            Id = system.Id,
            SystemName = system.SystemName,
            OrganizationName = system.OrganizationName,
            ISType = system.ISType,
            UID = system.UID,
            ISSMId = system.ISSMId
        };

        await PopulateUsersDropdownAsync(system.ISSMId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSystem(int id, SystemEditViewModel model)
    {
        if (id != model.Id)
            return NotFound();

        if (ModelState.IsValid)
        {
            var system = await _context.Systems.FindAsync(id);
            if (system == null)
                return NotFound();

            var oldValues = new { system.SystemName, system.OrganizationName, system.ISType, system.UID, system.ISSMId };

            system.SystemName = model.SystemName;
            system.OrganizationName = model.OrganizationName;
            system.ISType = model.ISType;
            system.UID = model.UID;
            system.ISSMId = model.ISSMId;

            await _context.SaveChangesAsync();

            var currentUser = await GetCurrentUserAsync();
            await _auditService.LogAsync(currentUser?.Id, "Update", "System", system.Id, oldValues, model, GetIpAddress());

            TempData["Success"] = $"System {system.SystemName} updated successfully.";
            return RedirectToAction(nameof(Systems));
        }

        await PopulateUsersDropdownAsync(model.ISSMId);
        return View(model);
    }

    // Audit Logs (Admin only)
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AuditLogs(DateTime? from, DateTime? to, string? entityType)
    {
        var logs = await _auditService.GetLogsAsync(from, to, entityType);

        ViewBag.EntityTypes = new SelectList(new[] { "User", "System", "POAM", "Milestone" });
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.EntityType = entityType;

        return View(logs);
    }

    // AD Configuration (Admin only)
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ADConfiguration()
    {
        var config = await _context.ADConfigurations.FirstOrDefaultAsync();
        var viewModel = config != null
            ? new ADConfigurationViewModel
            {
                Id = config.Id,
                LdapServer = config.LdapServer,
                LdapPort = config.LdapPort,
                UseSsl = config.UseSsl,
                Domain = config.Domain,
                BaseDN = config.BaseDN,
                ServiceAccountUsername = config.ServiceAccountUsername,
                IsEnabled = config.IsEnabled,
                AutoCreateUsers = config.AutoCreateUsers,
                DefaultNewUserRole = config.DefaultNewUserRole,
                LastConnectionTest = config.LastConnectionTest,
                LastConnectionSuccess = config.LastConnectionSuccess,
                LastConnectionError = config.LastConnectionError,
                HasPassword = !string.IsNullOrEmpty(config.ServiceAccountPasswordEncrypted)
            }
            : new ADConfigurationViewModel();

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ADConfiguration(ADConfigurationViewModel model)
    {
        // Password is only required for new config
        var existingConfig = await _context.ADConfigurations.FirstOrDefaultAsync();
        if (existingConfig == null && string.IsNullOrEmpty(model.ServiceAccountPassword))
        {
            ModelState.AddModelError("ServiceAccountPassword", "Password is required for initial configuration.");
        }

        if (!ModelState.IsValid)
            return View(model);

        if (existingConfig == null)
        {
            existingConfig = new ADConfiguration();
            _context.ADConfigurations.Add(existingConfig);
        }

        var oldValues = existingConfig.Id > 0 ? new
        {
            existingConfig.LdapServer,
            existingConfig.LdapPort,
            existingConfig.UseSsl,
            existingConfig.Domain,
            existingConfig.BaseDN,
            existingConfig.ServiceAccountUsername,
            existingConfig.IsEnabled,
            existingConfig.AutoCreateUsers,
            existingConfig.DefaultNewUserRole
        } : null;

        existingConfig.LdapServer = model.LdapServer;
        existingConfig.LdapPort = model.LdapPort;
        existingConfig.UseSsl = model.UseSsl;
        existingConfig.Domain = model.Domain;
        existingConfig.BaseDN = model.BaseDN;
        existingConfig.ServiceAccountUsername = model.ServiceAccountUsername;
        existingConfig.IsEnabled = model.IsEnabled;
        existingConfig.AutoCreateUsers = model.AutoCreateUsers;
        existingConfig.DefaultNewUserRole = model.DefaultNewUserRole;

        // Only update password if provided
        if (!string.IsNullOrEmpty(model.ServiceAccountPassword))
        {
            existingConfig.ServiceAccountPasswordEncrypted = _adService.EncryptPassword(model.ServiceAccountPassword);
        }

        var currentUser = await GetCurrentUserAsync();
        existingConfig.ModifiedById = currentUser?.Id;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(currentUser?.Id, oldValues == null ? "Create" : "Update", "ADConfiguration", existingConfig.Id, oldValues, model, GetIpAddress());

        TempData["Success"] = "AD configuration saved successfully.";
        return RedirectToAction(nameof(ADConfiguration));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> TestADConnection()
    {
        var result = await _adService.TestConnectionAsync();

        // Update last test info in config
        var config = await _context.ADConfigurations.FirstOrDefaultAsync();
        if (config != null)
        {
            config.LastConnectionTest = DateTime.UtcNow;
            config.LastConnectionSuccess = result.Success;
            config.LastConnectionError = result.ErrorMessage;
            await _context.SaveChangesAsync();
        }

        return Json(new
        {
            success = result.Success,
            message = result.Success
                ? $"Connected successfully in {result.ResponseTime.TotalMilliseconds:F0}ms"
                : result.ErrorMessage,
            serverInfo = result.ServerInfo
        });
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SearchADUsers(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return Json(new List<object>());

        var results = await _adService.SearchUsersAsync(term);
        return Json(results.Select(u => new
        {
            u.SamAccountName,
            u.DisplayName,
            u.Email,
            u.Department
        }));
    }

    private async Task PopulateUsersDropdownAsync(int? selectedId = null)
    {
        var issms = await _context.Users
            .Where(u => u.IsActive && (u.Role == UserRole.Admin || u.Role == UserRole.ISSM))
            .OrderBy(u => u.DisplayName)
            .ToListAsync();

        ViewBag.ISSMs = new SelectList(issms, "Id", "DisplayName", selectedId);
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        if (User.Identity?.Name == null) return null;
        return await _userService.GetByUsernameAsync(User.Identity.Name);
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
