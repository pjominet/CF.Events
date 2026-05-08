using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CF.Events.API.Data;
using CF.Events.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CF.Events.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/events/engagement/setup-status", async (EventsDbContext db) =>
        {
            var userExists = await db.Users.AnyAsync();
            return Results.Ok(new { needsSetup = !userExists });
        });

        app.MapPost("/api/events/engagement/setup", async (LoginRequest request, EventsDbContext db) =>
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
        });

        app.MapPost("/api/events/engagement/login", async (LoginRequest request, EventsDbContext db, IConfiguration config) =>
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
        });

        app.MapPost("/api/events/engagement/refresh", (ClaimsPrincipal user, IConfiguration config) =>
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
    }
}
