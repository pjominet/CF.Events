using System.Text.Json.Serialization;
using CF.Events.API.Data;
using CF.Events.API.Infrastructure;
using CF.Events.Shared;
using Microsoft.AspNetCore.Identity;
using static CF.Events.Shared.Constants;

namespace CF.Events.API;

public class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAppDatabases(configuration);
        services.AddAppAuthentication(configuration, environment);
        services.AddAppServices();
        services.AddAppRateLimiters(environment);
        services.AddAppDataProtection(environment);

        services.AddOpenApi();

        services.AddControllers().AddJsonOptions(options =>
        {
            // Serialize enums as strings for better readability
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
            });
        });

        services.AddAuthorization();
    }

    public async Task EnsureDatabaseSeeded(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = new[] { Roles.Admin };
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
            app.UseExceptionHandler(handler =>
            {
                handler.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
                });
            });
            app.UseHsts();
        }

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseCors();
        app.UseSecurityHeaders();

        if (!app.Environment.IsDevelopment())
            app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
    }
}
