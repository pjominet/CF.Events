using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using CF.Events.Shared.Models;
using CF.Events.API.Data;
using CF.Events.API.Services;
using CF.Events.Shared;
using static CF.Events.Shared.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "keys");
if (builder.Environment.IsProduction() && Directory.Exists("/app"))
    keysPath = "/app/keys";

if (!Directory.Exists(keysPath))
    Directory.CreateDirectory(keysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("CF.Events.API");

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        if (builder.Environment.IsDevelopment())
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

builder.Services.AddAuthentication(options =>
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
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

builder.Services.AddAuthorization();

builder.Services.AddHostedService<TokenCleanupService>();

builder.Services.AddDbContext<EventsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=events.db"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
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

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter(RateLimiting.Fixed, opt =>
        {
            opt.Window = TimeSpan.FromSeconds(10);
            opt.PermitLimit = 10;
            opt.QueueLimit = 0;
        });

        options.AddFixedWindowLimiter(RateLimiting.Strict, opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 5;
            opt.QueueLimit = 0;
        });
    });
}

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
    db.Database.EnsureCreated();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { Roles.Admin };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

// Configure the HTTP request pipeline.
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

app.Run();
