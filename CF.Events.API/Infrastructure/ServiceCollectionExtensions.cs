using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CF.Events.API.Data;
using CF.Events.API.Services;
using CF.Events.Shared;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CF.Events.API.Infrastructure;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddAppDatabases(IConfiguration configuration)
        {
            services.AddDbContext<EventsDbContext>(options
                => options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=events.db"));
        }

        public void AddAppAuthentication(IConfiguration configuration, IWebHostEnvironment environment)
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
                .AddDefaultTokenProviders();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var db = context.HttpContext.RequestServices.GetRequiredService<EventsDbContext>();
                            if (context.SecurityToken is JwtSecurityToken token && await db.RevokedTokens.AnyAsync(t => t.Token == token.RawData))
                                context.Fail("Token has been revoked.");
                        }
                    };
                });
        }

        public void AddAppServices()
        {
            services.AddScoped<TokenService>();
            services.AddHostedService<TokenCleanupService>();
        }

        public void AddAppRateLimiters(IWebHostEnvironment environment)
        {
            if (!environment.IsDevelopment())
            {
                services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                    options.AddFixedWindowLimiter(Constants.RateLimiting.Fixed, opt =>
                    {
                        opt.Window = TimeSpan.FromSeconds(10);
                        opt.PermitLimit = 10;
                        opt.QueueLimit = 0;
                    });

                    options.AddFixedWindowLimiter(Constants.RateLimiting.Strict, opt =>
                    {
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.PermitLimit = 5;
                        opt.QueueLimit = 0;
                    });
                });
            }
        }

        public void AddAppDataProtection(IWebHostEnvironment environment)
        {
            var keysPath = Path.Combine(environment.ContentRootPath, "keys");
            if (environment.IsProduction() && Directory.Exists("/app"))
                keysPath = "/app/keys";

            if (!Directory.Exists(keysPath))
                Directory.CreateDirectory(keysPath);

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
                .SetApplicationName("CF.Events.API");
        }
    }
}
