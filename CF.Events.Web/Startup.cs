using System.Text.Json;
using System.Text.Json.Serialization;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web;

public class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAppSettings(configuration);
        services.AddAppDatabases(configuration);
        services.AddAppServices();
        services.AddAppAuthentication(environment, configuration);
        services.AddAppDataProtection(environment);
        services.AddHttpClients(configuration);

        services.AddRazorPages(options =>
        {
            options.Conventions.Add(new PageRouteTransformerConvention(new PascalCaseRouteTransformer()));
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
            ExtendedTimeOut = 750
        });

        services.AddControllers();
        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = false;
        });
    }

    public async Task EnsureDatabase(IServiceProvider serviceProvider)
    {
        const int maxRetries = 10;
        const int retryDelaySeconds = 5;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

                // Test connection first
                await db.Database.OpenConnectionAsync();
                Console.WriteLine($"Connection test successful (attempt {attempt})");

                // Apply migrations
                Console.WriteLine("Applying database migrations...");
                await db.Database.MigrateAsync();
                Console.WriteLine("Migrations applied successfully");

                // Seed roles - with connection retry
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var roles = new[] { Roles.Admin, Roles.User };

                foreach (var role in roles)
                {
                    if (await roleManager.RoleExistsAsync(role))
                        continue;

                    Console.WriteLine($"Creating role: {role}");
                    await roleManager.CreateAsync(new IdentityRole(role));
                }

                Console.WriteLine("Database initialization complete");
                return; // Success - exit the method
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database initialization attempt {attempt} failed: {ex.Message}");

                if (attempt == maxRetries)
                {
                    Console.WriteLine("Max retries reached. Failing...");
                    throw; // This will crash the container, triggering Docker restart
                }

                await Task.Delay(retryDelaySeconds * 1000);
            }
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
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseUrlTransformer();
        app.MapControllers();
        app.MapRazorPages();

        app.MapGet("/api/generate-password", () => Results.Text(TempPasswordGenerator.Generate()))
            .RequireAuthorization();
    }
}
