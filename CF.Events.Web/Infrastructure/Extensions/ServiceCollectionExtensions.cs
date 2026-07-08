using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Exceptions;
using CF.Events.Web.Infrastructure.Factories;
using CF.Events.Web.Infrastructure.Providers;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using CF.Events.Web.Services.BackgroundWorkers;
using Microsoft.AspNetCore.DataProtection;
using Smtp2Go.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddAppDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(options
            => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")
                                    ?? throw new BootstrappingException("Missing DB connection string")));
    }

    public static void AddAppAuthentication(this IServiceCollection services, IWebHostEnvironment environment, IConfiguration configuration)
    {
        services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                if (environment.IsDevelopment())
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = configuration.GetSection("AppSettings:PasswordLength").Get<int>();
                }
                else
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = configuration.GetSection("AppSettings:PasswordLength").Get<int>();
                }

                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                options.Tokens.EmailConfirmationTokenProvider = ProviderNames.EmailConfirmation;
            })
            .AddEntityFrameworkStores<EventsDbContext>()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddTokenProvider<EmailConfirmationTokenProvider<AppUser>>(ProviderNames.EmailConfirmation);

        services.AddAuthorization();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/AccessDenied";
        });
    }

    public static void AddAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AppSettings>()
            .Bind(configuration.GetSection(nameof(AppSettings)))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AppSettings>, AppSettingsValidator>();
    }

    public static void AddAppServices(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddHostedService<InvitationEmailWorker>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<IExportService, ExportService>();

        if (environment.IsDevelopment())
        {
            services.AddScoped<IEmailSender<AppUser>, NoOpIdentitySender>();
            services.AddScoped<IMailService, NoOpMailService>();
        }
        else
        {
            services.AddScoped<IEmailProvider, Smtp2GoEmailProvider>();
            services.AddScoped<IEmailSender<AppUser>, IdentityEmailSender>();
            services.AddScoped<IMailService, MailService>();
        }
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
        services.AddScoped<IApiService, Smtp2GoApiService>(_ =>
        {
            var apiKey = configuration["AppSettings:EmailProviderSettings:ApiKey"];
            return new Smtp2GoApiService(apiKey);
        });
    }
}
