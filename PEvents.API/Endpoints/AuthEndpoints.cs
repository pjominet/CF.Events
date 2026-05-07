using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PEvents.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/events/engagement/login", (string password, IConfiguration config) =>
        {
            if (password == "admin")
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, "admin"),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: config["Jwt:Issuer"],
                    audience: config["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(15),
                    signingCredentials: creds
                );

                return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
            }
            return Results.Unauthorized();
        });

        app.MapPost("/api/events/engagement/refresh", (ClaimsPrincipal user, IConfiguration config) =>
        {
            if (user.Identity?.IsAuthenticated == true)
            {
                var claims = user.Claims.ToList();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: config["Jwt:Issuer"],
                    audience: config["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(15),
                    signingCredentials: creds
                );

                return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
            }
            return Results.Unauthorized();
        }).RequireAuthorization();
    }
}
