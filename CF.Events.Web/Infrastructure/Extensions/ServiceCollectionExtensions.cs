using System.Globalization;
using System.Threading.RateLimiting;
using AngleSharp.Html.Parser;
using Ganss.Xss;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Exceptions;
using CF.Events.Web.Infrastructure.Factories;
using CF.Events.Web.Infrastructure.Providers;
using CF.Events.Web.Infrastructure.Providers.Interfaces;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using CF.Events.Web.Services.BackgroundWorkers;
using CF.Events.Web.Services.EmailSenders;
using CF.Events.Web.Services.Interfaces;
using CF.Smtp2Go.Net;
using EditorJsonToHtmlConverter;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
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

    public static void AddAppServices(this IServiceCollection services, IWebHostEnvironment environment, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddScopedEditorJsonProcessorServices();
        services.AddScoped<IHtmlParser, HtmlParser>();
        services.AddSingleton<IAssetIntegrityService, AssetIntegrityService>();

        services.AddHostedService<InvitationEmailWorker>();
        services.AddHostedService<EmailActivityWorker>();

        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IAuthEmailService, AuthEmailService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IEmailActivityService, EmailActivityService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();

        var hasEmailProviderApiKey = configuration.GetSection("AppSettings:EmailProviderSettings:Smtp2Go:ApiKey").Get<string>().HasValue();
        if (environment.IsDevelopment() && !hasEmailProviderApiKey)
        {
            Log.Information("Using mock email service!");
            services.AddScoped<IIdentityEmailSender, NoOpIdentitySender>();
            services.AddScoped<IEmailSender<AppUser>>(sp => sp.GetRequiredService<IIdentityEmailSender>());
            services.AddScoped<IEmailSender, NoOpEmailSender>();
        }
        else
        {
            if (environment.IsDevelopment())
                Log.Information("Using sandboxed Smtp2Go email provider!");
            else Log.Information("Using Smtp2Go email provider!");

            services.AddScoped<IEmailProvider, Smtp2GoEmailProvider>();
            services.AddScoped<IIdentityEmailSender, IdentityEmailSender>();
            services.AddScoped<IEmailSender<AppUser>>(sp => sp.GetRequiredService<IIdentityEmailSender>());
            services.AddScoped<IEmailSender, EmailSender>();
        }
    }

    public static void AddAppLocalization(this IServiceCollection services)
    {
        var culture = new CultureInfo("en-GB")
        {
            DateTimeFormat =
            {
                ShortTimePattern = "HH:mm",
                LongTimePattern = "HH:mm:ss",
                ShortDatePattern = "dd/MM/yyyy"
            }
        };

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(culture);
            options.SupportedCultures = [culture];
            options.SupportedUICultures = [culture];
        });
    }

    public static void AddAppDataProtection(this IServiceCollection services, IWebHostEnvironment environment)
    {
        var keysPath = Path.Combine(environment.ContentRootPath, "keys");
        if (!environment.IsDevelopment() && Directory.Exists("/app"))
            keysPath = "/app/keys";

        if (!Directory.Exists(keysPath))
            Directory.CreateDirectory(keysPath);

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("CF.Events.Web");
    }

    public static void AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitingPolicy.EmailLogin, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });
    }

    public static void AddHttpClients(this IServiceCollection services, IWebHostEnvironment environment, IConfiguration configuration)
    {
        var apiKey = configuration.GetSection("AppSettings:EmailProviderSettings:Smtp2Go:ApiKey").Get<string>();
        if (!apiKey.HasValue() && !environment.IsDevelopment())
            throw new BootstrappingException("Email provider API key is not configured");

        services.AddHttpClient<ISmtp2GoClient, Smtp2GoClient>(client =>
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("X-Smtp2go-Api-Key", apiKey);
            client.BaseAddress = new Uri("https://api.smtp2go.com/v3/");
        });
    }

    public static void AddAppSanitization(this IServiceCollection services)
    {
        var options = new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string> { "a", "p", "b", "i", "br", "hr" },
            AllowedAttributes = new HashSet<string> { "href", "target", "rel" },
            UriAttributes = new HashSet<string> { "href" },
            AllowedSchemes = new HashSet<string> { "https", "mailto" }
        };

        services.AddSingleton<IHtmlSanitizer>(_ => new HtmlSanitizer(options));
    }
}
