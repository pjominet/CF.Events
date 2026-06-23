using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Factories;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Mailjet.Client;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddAppDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(options
            => options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=events.db"));
    }

    public static void AddAppAuthentication(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                if (environment.IsDevelopment())
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;
                }
                else
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 8;
                }

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<EventsDbContext>()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddAuthorization();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        if (environment.IsDevelopment())
            services.AddSingleton<IEmailSender<ApplicationUser>, NoOpEmailSender>();
        else services.AddSingleton<IEmailSender<ApplicationUser>, MailjetEmailSender>();
    }

    public static void AddAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)));
    }

    public static void AddAppServices(this IServiceCollection services)
    {
    }

    public static void AddAppDataProtection(this IServiceCollection services, IWebHostEnvironment environment)
    {
        var keysPath = Path.Combine(environment.ContentRootPath, "keys");
        if (environment.IsProduction() && Directory.Exists("/app"))
            keysPath = "/app/keys";

        if (!Directory.Exists(keysPath))
            Directory.CreateDirectory(keysPath);

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("CF.Events.Web");
    }

    public static void AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IMailjetClient, MailjetClient>(client =>
        {
            //set BaseAddress, MediaType, UserAgent
            client.SetDefaultSettings();

            var apiKey = configuration["AppSettings:Mailjet:ApiKey"];
            var apiSecret = configuration["AppSettings:Mailjet:ApiSecret"];
            client.UseBasicAuthentication(apiKey, apiSecret);
        });
    }
}
