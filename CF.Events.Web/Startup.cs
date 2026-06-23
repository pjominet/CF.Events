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
        services.AddAppDatabases(environment, configuration);
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
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            // Simple retry logic without Polly
            const int maxRetries = 5;
            const int retryDelaySeconds = 5;

            for (var i = 0; i < maxRetries; i++)
            {
                try
                {
                    await db.Database.MigrateAsync();
                    Console.WriteLine("Database migrations applied successfully");
                    break; // Success - exit retry loop
                }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    Console.WriteLine($"Migration attempt {i + 1} failed: {ex.Message}. Retrying in {retryDelaySeconds} seconds...");
                    await Task.Delay(retryDelaySeconds * 1000);
                }
            }

            // Seed roles
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var roles = new[] { Roles.Admin, Roles.User };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EnsureDatabase failed: {ex}");
            throw; // Re-throw to fail the container
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
