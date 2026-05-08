using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CF.Events.API.Data;
using CF.Events.API.Models;
using CF.Events.Shared;
using CF.Events.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CF.Events.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events/engagement").RequireRateLimiting(Constants.RateLimiting.Fixed);

        group.MapGet("/setup-status", async (EventsDbContext db) =>
        {
            var userExists = await db.Users.AnyAsync();
            return Results.Ok(new { needsSetup = !userExists });
        });

        group.MapPost("/setup", async (LoginRequest request, EventsDbContext db) =>
        {
            if (await db.Users.AnyAsync())
                return Results.BadRequest("User already exists.");

            var admin = new User
            {
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).RequireRateLimiting(Constants.RateLimiting.Strict);

        group.MapPost("/login", async (LoginRequest request, EventsDbContext db, IConfiguration config) =>
        {
            var user = await db.Users.FirstOrDefaultAsync();

            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Results.Unauthorized();

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "admin")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                config["Jwt:Issuer"],
                config["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: credentials
            );

            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }).RequireRateLimiting(Constants.RateLimiting.Strict);

        group.MapPost("/refresh", (ClaimsPrincipal user, IConfiguration config) =>
        {
            if (user.Identity?.IsAuthenticated is not true)
                return Results.Unauthorized();

            var claims = user.Claims.ToList();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                config["Jwt:Issuer"],
                config["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: credentials
            );

            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }).RequireAuthorization();

        group.MapPost("/logout", async (HttpContext context, EventsDbContext db) =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
                return Results.BadRequest("Invalid token");

            var jwtToken = handler.ReadJwtToken(token);

            if (await db.RevokedTokens.AnyAsync(t => t.Token == token))
                return Results.NoContent();

            db.RevokedTokens.Add(new RevokedToken
            {
                Token = token,
                ExpiryDate = jwtToken.ValidTo
            });

            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();
    }
}
