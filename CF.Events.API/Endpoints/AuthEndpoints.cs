using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CF.Events.API.Data;
using CF.Events.API.Models;
using CF.Events.Shared;
using CF.Events.Shared.DTOs;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CF.Events.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").RequireRateLimiting(Constants.RateLimiting.Fixed);

        group.MapPost("/register", async (RegisterRequest request, UserManager<ApplicationUser> userManager) =>
        {
            var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return Results.BadRequest(new AuthResponse
                {
                    Success = false,
                    Error = string.Join(", ", result.Errors.Select(e => e.Description))
                });
            }

            // By default, first user is Admin, others are User
            var isFirstUser = await userManager.Users.CountAsync() == 1;
            await userManager.AddToRoleAsync(user, isFirstUser ? Constants.Roles.Admin : Constants.Roles.User);

            return Results.Ok(new AuthResponse { Success = true });
        }).RequireRateLimiting(Constants.RateLimiting.Strict);

        group.MapPost("/login", async (LoginRequest request, UserManager<ApplicationUser> userManager, IConfiguration config) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Email!),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                config["Jwt:Issuer"],
                config["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials
            );

            return Results.Ok(new AuthResponse
            {
                Success = true,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Email = user.Email,
                Roles = roles
            });
        }).RequireRateLimiting(Constants.RateLimiting.Strict);

        group.MapGet("/me", async (ClaimsPrincipal claimsPrincipal, UserManager<ApplicationUser> userManager) =>
        {
            var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null) return Results.NotFound();

            var roles = await userManager.GetRolesAsync(user);

            return Results.Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                Roles = roles
            });
        }).RequireAuthorization();

        group.MapPost("/change-password", async (UpdatePasswordRequest request, ClaimsPrincipal claimsPrincipal, UserManager<ApplicationUser> userManager) =>
        {
            var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null) return Results.NotFound();

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new AuthResponse
                {
                    Success = false,
                    Error = string.Join(", ", result.Errors.Select(e => e.Description))
                });
            }

            return Results.Ok(new AuthResponse { Success = true });
        }).RequireAuthorization();

        group.MapPost("/logout", async (HttpContext context, EventsDbContext db) =>
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            var token = authHeader["Bearer ".Length..].Trim();
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
