using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[Route("events")]
public class EventController(
    EventsDbContext db,
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IMailService mailService,
    IToastNotification toastNotification,
    ILogger<EventController> logger,
    IWebHostEnvironment env) : Controller
{
    [HttpGet("{eventId:int}/asset")]
    public async Task<IActionResult> GetInvitationAsset([FromRoute] int eventId)
    {
        var userId = User.GetId();
        var isInvited = await db.UserEvents.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
        if (!isInvited && !User.IsAdmin())
            return Forbid();

        var ev = await db.Events.FindAsync(eventId);
        if (ev is null || string.IsNullOrEmpty(ev.InvitationFileName))
            return NotFound();

        // The full path is built dynamically from the event ID (folder) and the
        // stored technical file name.
        var invitationsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Resources", "Invitations"));
        var requested = Path.GetFullPath(Path.Combine(invitationsRoot, eventId.ToString(), ev.InvitationFileName));

        // Prevent path traversal outside the invitations folder.
        if (!requested.StartsWith(invitationsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return Forbid();

        if (!System.IO.File.Exists(requested))
            return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(requested, out var contentType))
            contentType = "application/octet-stream";

        return PhysicalFile(requested, contentType);
    }

    [HttpPost("{eventId:int}/invite-users")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> InviteUsers([FromRoute] int eventId, [FromForm] BulkInvite invite)
    {
        var eventData = await db.Events
            .Where(e => e.Id == eventId && e.IsActive)
            .Include(e => e.EventUsers)
            .Include(e => e.InviteCodes)
            .FirstOrDefaultAsync();

        if (eventData is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage($"/admin/events/{eventId}/invitees");
        }

        var validCode = eventData.InviteCodes
            .Where(c => c.Code == invite.InviteCode && c.ValidUntil > DateTime.UtcNow)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => c.Code)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(validCode))
        {
            toastNotification.AddWarningToastMessage("Invalid or expired invite code");
            return RedirectToPage($"/admin/events/{eventId}/invitees");
        }

        var existingUserIds = eventData.EventUsers.Select(eu => eu.UserId).ToHashSet();
        var newUserIds = invite.UserIds.Where(userid => !existingUserIds.Contains(userid)).ToList();

        foreach (var userId in newUserIds)
        {
            eventData.EventUsers.Add(new UserEvent
            {
                EventId = eventId,
                UserId = userId,
                AssignedAccommodationCode = invite.AllowUseOfAccommodationCode ? eventData.AccommodationCode : null
            });
        }

        try
        {
            var count = await db.SaveChangesAsync();

            if (invite.SendEmailsOnInvite)
            {
                var newUsers = await db.Users
                    .Where(u => newUserIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var user in newUsers)
                {
                    logger.LogInformation("Sending invitation to {Email}", user.Email);
                    var callbackUrl = Url.Action("InvitationCallback", "Event", new { code = invite.InviteCode, email = user.Email }, Request.Scheme);
                    await mailService.SendInvitationAsync(eventData.Name, user.DisplayName!, user.Email!, callbackUrl!);
                }
            }

            toastNotification.AddSuccessToastMessage($"Successfully created {count} invitations");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }
        catch
        {
            toastNotification.AddErrorToastMessage("Invitations could not be created");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }
    }

    [HttpPost("{eventId:int}/resend-invite")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ResendInvite([FromRoute] int eventId, [FromForm] string userId, [FromForm] string inviteCode)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            toastNotification.AddWarningToastMessage("User not found");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var isInvited = await db.UserEvents.AnyAsync(eu => eu.EventId == eventId && eu.UserId == user.Id);
        if (!isInvited)
        {
            toastNotification.AddWarningToastMessage("User is not invited to this event");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var eventData = await db.Events.FindAsync(eventId);
        if (eventData is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        try
        {
            logger.LogInformation("Re-sending invitation to {Email}", user.Email);
            var callbackUrl = Url.Action("InvitationCallback", "Event", new { code = inviteCode, email = user.Email }, Request.Scheme);
            await mailService.SendInvitationAsync(eventData.Name, user.DisplayName!, user.Email!, callbackUrl!);

            toastNotification.AddSuccessToastMessage("Successfully resent invitation");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }
        catch
        {
            toastNotification.AddErrorToastMessage("Invitations could not be created");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }
    }

    [HttpGet("invite-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> InvitationCallback([FromQuery] string code, [FromQuery] string email)
    {
        var inviteCode = await db.InviteCodes.FirstOrDefaultAsync(c => c.Code == code && c.ValidUntil > DateTime.UtcNow);
        if (inviteCode is null)
        {
            logger.LogWarning("Invalid or expired invite code was used: {Code}", code);
            return BadRequest();
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            logger.LogWarning("User with email {Email} not found or inactive", email);
            return BadRequest();
        }

        var isInvited = await db.UserEvents.AnyAsync(eu => eu.EventId == inviteCode.EventId && eu.UserId == user.Id);
        if (!isInvited)
        {
            logger.LogWarning("User with email {Email} was not invited to event {EventId}", email, inviteCode.EventId);
            return BadRequest();
        }

        if (signInManager.IsSignedIn(User) && User.Identity?.Name == email)
            return LocalRedirect("/");

        await signInManager.SignInAsync(user, isPersistent: true);

        if (await userManager.HasPasswordAsync(user) && !user.MustChangePassword)
            return LocalRedirect("/");

        return RedirectToPage("/account/manage/FirstLogin");
    }

    [HttpPost("{eventId:int}/regenerate-code")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> RegenerateCode([FromRoute] int eventId, [FromForm] int validDays)
    {
        // Try to use referrer for redirect if possible, otherwise default to events list
        var referrer = Request.Headers.Referer.ToString();
        var eventExists = await db.Events.AnyAsync(e => e.Id == eventId);
        if (!eventExists)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            if (!string.IsNullOrEmpty(referrer))
                return Redirect(referrer);
            return RedirectToPage("/admin/events");
        }

        var newCode = new InviteCode
        {
            EventId = eventId,
            Code = CodeGenerator.Generate(64),
            ValidUntil = DateTime.UtcNow.AddDays(validDays),
            CreatedAt = DateTime.UtcNow
        };

        db.InviteCodes.Add(newCode);
        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("New invite code generated");

        if (!string.IsNullOrEmpty(referrer))
            return Redirect(referrer);
        return RedirectToPage("/admin/events");
    }
}
