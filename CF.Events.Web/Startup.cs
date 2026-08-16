using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Infrastructure.Filters;
using CF.Events.Web.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using Serilog;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web;

public class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAppSettings(configuration);
        services.AddAppDatabases(configuration);
        services.AddAppServices(environment);
        services.AddAppAuthentication(environment, configuration);
        services.AddAppDataProtection(environment);
        services.AddAppRateLimiting();
        services.AddHttpClients(configuration);

        services.AddRazorPages(options =>
            {
                options.Conventions.Add(new PageRouteTransformerConvention(new PascalCaseRouteTransformer()));
            })
            .AddMvcOptions(options =>
            {
                options.Filters.Add<InitPasswordFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            })
            .AddNToastNotifyToastr(new ToastrOptions
            {
                ProgressBar = true,
                PositionClass = ToastPositions.TopRight,
                TapToDismiss = true,
                TimeOut = 5000,
                ExtendedTimeOut = 750,
                ShowMethod = "fadeIn",
                HideMethod = "fadeOut"
            });

        services.AddControllers(options =>
            {
                options.Filters.Add<InitPasswordFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            })
            .AddNToastNotifyToastr(new ToastrOptions
            {
                ProgressBar = true,
                PositionClass = ToastPositions.TopRight,
                TapToDismiss = true,
                TimeOut = 5000,
                ExtendedTimeOut = 750,
                ShowMethod = "fadeIn",
                HideMethod = "fadeOut"
            });

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = false;
        });

        SetDefaultCulture();
    }

    public async Task EnsureDatabase(IServiceProvider serviceProvider, CancellationToken ctx = default)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            // Apply migrations
            Log.Information("Applying database migrations...");
            await db.Database.MigrateAsync(cancellationToken: ctx);
            Log.Information("Migrations applied successfully");

            // Seed roles
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var roles = new[] { Roles.Admin, Roles.User, Roles.Guest };

            Log.Information("Seeding roles...");
            foreach (var role in roles)
            {
                if (await roleManager.RoleExistsAsync(role))
                {
                    Log.Information("Role {Role} already exists, continuing...", role);
                    continue;
                }

                Log.Information("Creating role: {Role}", role);
                await roleManager.CreateAsync(new IdentityRole(role));
            }

            Log.Information("Database initialization complete");
        }
        catch (Exception ex)
        {
            Log.Error("Database initialization attempt failed: {Message}", ex.Message);
            throw;
        }
    }

    public void Configure(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/error", "?code={0}");

        app.UseSecurityHeaders();
        app.UseRateLimiter();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseSession();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseUrlTransformer();
        app.MapControllers();
        app.MapRazorPages();
    }

    private static void SetDefaultCulture()
    {
        var cultureInfo = new CultureInfo("en-UK");
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
    }
}
