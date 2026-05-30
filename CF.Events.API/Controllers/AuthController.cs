using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CF.Events.API.Data;
using CF.Events.API.Infrastructure.Attributes;
using CF.Events.API.Models;
using CF.Events.API.Services;
using CF.Events.Shared.DTOs;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using static CF.Events.Shared.Constants;

namespace CF.Events.API.Controllers;

[EnableRateLimiting(RateLimiting.Fixed)]
[ApiRoute("auth")]
public class AuthController(UserManager<ApplicationUser> userManager, TokenService tokenService, EventsDbContext dbContext, ILogger<AuthController> logger) : ApiController
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var firstLaunch = !await dbContext.Users.AnyAsync();

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to register user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new RegisterResponse { Success = false });
        }

        if (!firstLaunch) return Ok(new RegisterResponse { Success = true });

        logger.LogInformation("First launch detected. Adding admin role to user {Email}", request.Email);
        await userManager.AddToRoleAsync(user, Roles.Admin);

        return Ok(new RegisterResponse { Success = true });
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimiting.Strict)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            logger.LogWarning("Login failed: User not found for email {Email}", request.Email);
            return Unauthorized();
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            logger.LogWarning("Login failed: Invalid password for user {Email}", request.Email);
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);

        return Ok(new LoginResponse
        {
            Success = true,
            Token = tokenService.GenerateAccessToken(user, roles),
            RefreshToken = tokenService.GenerateRefreshToken(),
            Email = user.Email,
            MustChangePassword = user.MustChangePassword
        });
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

        if (await dbContext.RevokedTokens.AnyAsync(t => t.Token == token))
            return NoContent();

        dbContext.RevokedTokens.Add(new RevokedToken
        {
            Token = token,
            ExpiryDate = jwtToken.ValidTo
        });

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpPost("setup-account")]
    public async Task<IActionResult> SetupAccount(SetupAccountRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        if (!user.MustChangePassword)
            return BadRequest("Account is already set up.");

        // 1. Change password
        var passResult = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!passResult.Succeeded)
            return BadRequest(new AuthResponse { Success = false, Error = string.Join(", ", passResult.Errors.Select(e => e.Description)) });

        // 2. Clear flag
        user.MustChangePassword = false;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new AuthResponse { Success = false, Error = string.Join(", ", result.Errors.Select(e => e.Description)) });

        return Ok(new AuthResponse { Success = true });
    }

    [HttpGet("users")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetUsers()
    {
        var users = await dbContext.Users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email!,
            DisplayName = u.DisplayName!,
            Roles = dbContext.UserRoles.Where(ur => ur.UserId == u.Id)
                .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                .ToList()
        }).ToListAsync();

        return Ok(users);
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
}
