using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Middlewares;
using Microsoft.AspNetCore.Identity;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web;

public class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAppSettings(configuration);
        services.AddAppDatabases(configuration);
        services.AddAppServices();
        services.AddAppAuthentication(environment);
        services.AddAppDataProtection(environment);

        services.AddRazorPages();
        services.AddControllersWithViews();
        services.AddRouting(options => options.LowercaseUrls = true);
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
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        if (app.Environment.IsDevelopment())
            app.UseMigrationsEndPoint();

        app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");

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
