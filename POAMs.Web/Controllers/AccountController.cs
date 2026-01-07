using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POAMs.Web.Models.ViewModels;
using POAMs.Web.Services;

namespace POAMs.Web.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    public AccountController(IUserService userService, IAuditService auditService)
    {
        _userService = userService;
        _auditService = auditService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userService.GetByUsernameAsync(model.Username);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            await Task.Delay(1000); // Slow down brute force attempts
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account has been deactivated.");
            return View(model);
        }

        if (user.IsWindowsAuth)
        {
            ModelState.AddModelError(string.Empty, "This account uses Windows authentication. Please use your domain credentials.");
            return View(model);
        }

        var isValid = await _userService.ValidatePasswordAsync(user, model.Password);
        if (!isValid)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            await Task.Delay(1000); // Slow down brute force attempts
            return View(model);
        }

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        await _auditService.LogAsync(user.Id, "Login", "User", user.Id, null, null, GetIpAddress());

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                await _auditService.LogAsync(userId, "Logout", "User", userId, null, null, GetIpAddress());
            }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = NegotiateDefaults.AuthenticationScheme)]
    public IActionResult WindowsLogin(string? returnUrl = null)
    {
        // The Windows authentication middleware will handle creating the cookie session
        // This action triggers the Negotiate challenge and redirects after success

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login");
        }

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        return View(user);
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return RedirectToAction("Login");
        }

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        if (user.IsWindowsAuth)
        {
            TempData["Error"] = "Cannot change password for Windows authentication accounts.";
            return RedirectToAction("Profile");
        }

        var isValid = await _userService.ValidatePasswordAsync(user, model.CurrentPassword);
        if (!isValid)
        {
            ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
            return View(model);
        }

        try
        {
            await _userService.UpdatePasswordAsync(user, model.NewPassword);
            await _auditService.LogAsync(userId, "ChangePassword", "User", userId, null, null, GetIpAddress());
            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction("Profile");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("NewPassword", ex.Message);
            return View(model);
        }
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
