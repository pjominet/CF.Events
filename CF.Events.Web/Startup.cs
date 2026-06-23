using System.Text.Json;
using System.Text.Json.Serialization;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
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

    public async Task EnsureDatabaseSeeded(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = new[] { Roles.Admin, Roles.User };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    public void Configure(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        if (app.Environment.IsDevelopment())
            app.UseMigrationsEndPoint();

        app.UseStatusCodePagesWithReExecute("/error", "?code={0}");

        app.UseSecurityHeaders();

        if (!app.Environment.IsDevelopment())
            app.UseRateLimiter();

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
