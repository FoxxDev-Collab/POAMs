using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using POAMs.Web.Services;

namespace POAMs.Web.Middleware;

public class WindowsAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WindowsAuthenticationMiddleware> _logger;

    public WindowsAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<WindowsAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IUserService userService,
        IActiveDirectoryService adService,
        IConfiguration configuration)
    {
        // Skip if already authenticated via cookie
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.Identity.AuthenticationType == CookieAuthenticationDefaults.AuthenticationScheme)
        {
            await _next(context);
            return;
        }

        // Check if Windows auth is enabled
        var useWindowsAuth = configuration.GetValue<bool>("Authentication:UseWindowsAuth");
        if (!useWindowsAuth)
        {
            await _next(context);
            return;
        }

        // Check if we have a Windows identity from Negotiate auth
        var windowsIdentity = context.User.Identity as WindowsIdentity;
        if (windowsIdentity == null || !windowsIdentity.IsAuthenticated)
        {
            await _next(context);
            return;
        }

        var windowsUsername = windowsIdentity.Name;
        if (string.IsNullOrEmpty(windowsUsername))
        {
            await _next(context);
            return;
        }

        _logger.LogDebug("Processing Windows auth for {Username}", windowsUsername);

        // Try to get/create user in our database
        Models.Domain.User? user = null;

        // First check if user exists (fast path, no AD needed)
        user = await userService.GetByUsernameAsync(windowsUsername);

        if (user == null)
        {
            // New user - check if AD is available for user info lookup
            var adConfig = await adService.GetConfigurationAsync();
            if (adConfig?.IsEnabled == true && adConfig.AutoCreateUsers)
            {
                if (await adService.IsAvailableAsync())
                {
                    var adInfo = await adService.GetUserByWindowsIdentityAsync(windowsUsername);
                    if (adInfo != null)
                    {
                        user = await userService.GetOrCreateWindowsUserAsync(windowsUsername, adInfo);
                        _logger.LogInformation("Created new Windows user {Username} from AD", windowsUsername);
                    }
                    else
                    {
                        _logger.LogWarning("User {Username} not found in AD", windowsUsername);
                    }
                }
                else
                {
                    // AD unavailable - deny new users
                    _logger.LogWarning("AD unavailable, denying new Windows user {Username}", windowsUsername);
                    context.Response.StatusCode = 503;
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(
                        "Active Directory is currently unavailable. Please contact your administrator or try again later.");
                    return;
                }
            }
            else if (adConfig?.AutoCreateUsers == false)
            {
                // Auto-create disabled, user must exist
                _logger.LogWarning("Auto-create users disabled, denying new Windows user {Username}", windowsUsername);
            }
            else
            {
                // AD not configured but Windows auth enabled - create user without AD info
                user = await userService.GetOrCreateWindowsUserAsync(windowsUsername);
                _logger.LogInformation("Created new Windows user {Username} (AD not configured)", windowsUsername);
            }
        }

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("User {Username} not found or inactive", windowsUsername);
            await _next(context);
            return;
        }

        // Create claims and sign in with cookie
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("AuthType", "Windows")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("Windows user {Username} signed in successfully", windowsUsername);

        await _next(context);
    }
}

public static class WindowsAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseWindowsAuthenticationHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<WindowsAuthenticationMiddleware>();
    }
}
