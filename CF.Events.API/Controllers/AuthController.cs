using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CF.Events.API.Data;
using CF.Events.API.Models;
using CF.Events.Shared.DTOs;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using static CF.Events.Shared.Constants;

namespace CF.Events.API.Controllers;

[Route("auth")]
[EnableRateLimiting(RateLimiting.Fixed)]
public class AuthController(UserManager<ApplicationUser> userManager, IConfiguration config, EventsDbContext db) : ApiController
{
    [HttpPost("register")]
    [EnableRateLimiting(RateLimiting.Strict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Error = string.Join(", ", result.Errors.Select(e => e.Description))
            });
        }

        // By default, first user is Admin, others are User
        var isFirstUser = await userManager.Users.CountAsync() is 1;
        await userManager.AddToRoleAsync(user, isFirstUser ? Roles.Admin : Roles.User);

        return Ok(new AuthResponse { Success = true });
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimiting.Strict)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized();
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

        return Ok(new AuthResponse
        {
            Success = true,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email,
            Roles = roles
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            Roles = roles
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(UpdatePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Error = string.Join(", ", result.Errors.Select(e => e.Description))
            });
        }

        return Ok(new AuthResponse { Success = true });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized();

        var token = authHeader["Bearer ".Length..].Trim();
        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(token))
            return BadRequest("Invalid token");

        var jwtToken = handler.ReadJwtToken(token);

        if (await db.RevokedTokens.AnyAsync(t => t.Token == token))
            return NoContent();

        db.RevokedTokens.Add(new RevokedToken
        {
            Token = token,
            ExpiryDate = jwtToken.ValidTo
        });

        await db.SaveChangesAsync();
        return NoContent();
    }
}
