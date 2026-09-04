using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NToastNotify;

namespace CF.Events.Web.Controllers;

[Route("account")]
public class AccountController(
    EventsDbContext db,
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IToastNotification toastNotification,
    IOptions<AppSettings> appOptions,
    ILogger<AccountController> logger) : Controller
{
    private readonly AppSettings _appSettings = appOptions.Value;

    [HttpGet("logout")]
    public async Task<IActionResult> Logout([FromQuery] string? returnUrl = null)
    {
        var userEmail = User.GetEmail();
        var isGuest = User.IsGuest();
        await signInManager.SignOutAsync();
        logger.LogInformation("User {Username} logged out", userEmail);

        if (returnUrl.HasValue(false) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return !isGuest ? LocalRedirect("/account/login") : LocalRedirect("/account/email-login");
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        if (!userId.HasValue() || !token.HasValue())
            return BadRequest();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        var result = await userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
            return RedirectToPage("/error");

        await signInManager.SignInAsync(user, isPersistent: true);

        toastNotification.AddSuccessToastMessage("Email successfully confirmed");
        return LocalRedirect("/");
    }

    [HttpGet("auth-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> InvitationCallback([FromQuery] string code, [FromQuery] int? eventId)
    {
        // Prevent leaking the token via Referer header
        Response.Headers.Append("Referrer-Policy", "no-referrer");

        var authCode = await db.AuthCodes
            .FirstOrDefaultAsync(c => c.Value == code && c.ValidUntil > DateTime.UtcNow);

        if (authCode is null || (eventId.HasValue && authCode.EventId != eventId))
        {
            logger.LogWarning("Invalid, expired or event-mismatched invite code was used: {Code}", code);
            return BadRequest();
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == authCode.UserId);

        if (user is null)
        {
            logger.LogWarning("User for invite code not found: {Code}", code);
            return BadRequest();
        }

        if (!user.IsActive)
        {
            logger.LogWarning("User with id {Id} is inactive", user.Id);
            return BadRequest();
        }

        // Invalidate the code immediately after successful retrieval
        db.AuthCodes.Remove(authCode);
        await db.SaveChangesAsync();

        var isGuest = await signInManager.UserManager.IsInRoleAsync(user, Constants.Roles.Guest);
        await signInManager.SignInAsync(user, new AuthenticationProperties
        {
            IsPersistent = isGuest,
            ExpiresUtc = isGuest ? DateTimeOffset.UtcNow.AddMonths(_appSettings.GuestLoginValidityMonths) : null
        });

        // Log the login audit
        db.LoginAudits.Add(new LoginAudit
        {
            UserId = user.Id,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            AuthMethod = "EmailToken"
        });
        await db.SaveChangesAsync();

        return LocalRedirect(eventId.HasValue ? $"/events/{eventId}/invitation" : "/");
    }
}
