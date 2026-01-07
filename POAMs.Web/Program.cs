using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Middleware;
using POAMs.Web.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/poams-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting POAMs web application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Add Data Protection for encrypting AD service account password
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
        .SetApplicationName("POAMs.Web");

    // Add DbContext with SQL Server
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Check if Windows auth is enabled
    var useWindowsAuth = builder.Configuration.GetValue<bool>("Authentication:UseWindowsAuth");

    // Configure authentication schemes
    var authBuilder = builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = useWindowsAuth
            ? NegotiateDefaults.AuthenticationScheme
            : CookieAuthenticationDefaults.AuthenticationScheme;
    });

    // Add cookie authentication
    authBuilder.AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

    // Add Negotiate (Windows) authentication if enabled
    if (useWindowsAuth)
    {
        authBuilder.AddNegotiate();
    }

    // Add authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("ManagerOrAbove", policy => policy.RequireRole("Admin", "ISSM"));
        options.AddPolicy("UserOrAbove", policy => policy.RequireRole("Admin", "ISSM", "ISSO", "SysAdmin"));
        options.AddPolicy("CanView", policy => policy.RequireRole("Admin", "ISSM", "ISSO", "SysAdmin", "Auditor"));
        options.AddPolicy("CanEdit", policy => policy.RequireRole("Admin", "ISSM", "ISSO", "SysAdmin"));
    });

    // Add services
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<IExportService, XactaExportService>();
    builder.Services.AddScoped<IImportService, ExcelImportService>();
    builder.Services.AddScoped<ISTPService, STPService>();
    builder.Services.AddScoped<INISTCatalogService, NISTCatalogService>();
    builder.Services.AddScoped<IDocumentationService, DocumentationService>();
    builder.Services.AddScoped<IDataImportService, DataImportService>();

    builder.Services.AddControllersWithViews(options =>
        {
            // Don't treat non-nullable reference types as required by default
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });

    // Add anti-forgery
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

    var app = builder.Build();

    // Apply migrations and seed data
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        await DbInitializer.InitializeAsync(db);
    }

    // Configure the HTTP request pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    // Security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });

    app.UseRouting();

    app.UseAuthentication();

    // Add Windows auth handler after authentication but before authorization
    if (useWindowsAuth)
    {
        app.UseWindowsAuthenticationHandler();
    }

    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
