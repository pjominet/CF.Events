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
    IInvitationService invitationService,
    IToastNotification toastNotification,
    ILogger<EventController> logger,
    IWebHostEnvironment env) : Controller
{
    [HttpGet("{eventId:int}/asset")]
    public async Task<IActionResult> GetInvitationAsset([FromRoute] int eventId)
    {
        var userId = User.GetId();
        var isInvited = await db.EventUsers.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
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
    public async Task<IActionResult> InviteUsers([FromRoute] int eventId, [FromForm] UserInvites invite)
    {
        // Validate event exists and is active
        var eventExists = await db.Events.AnyAsync(e => e.Id == eventId && e.IsActive);
        if (!eventExists)
        {
            toastNotification.AddWarningToastMessage("Event not found or not active anymore");
            return RedirectToPage($"/admin/events/{eventId}/invitees");
        }

        // Get only the invite codes needed for validation
        var existingCodes = await db.InviteCodes
            .Where(c => c.EventId == eventId)
            .ToHashSetAsync();

        if (!IsValidCode(existingCodes, invite.InviteCode, out var validCode))
        {
            toastNotification.AddWarningToastMessage("Invalid or expired invite code");
            return RedirectToPage($"/admin/events/{eventId}/invitees");
        }

        // Get users who are already invited to this event
        var existingUserIds = await db.EventUsers
            .Where(ue => ue.EventId == eventId)
            .Select(ue => ue.UserId)
            .ToListAsync();

        // Filter to only new users (not already invited)
        var newUserIds = invite.UserIds
            .Where(userId => !existingUserIds.Contains(userId))
            .ToList();

        if (newUserIds.Count == 0)
        {
            toastNotification.AddWarningToastMessage("All selected users are already invited to this event");
            return RedirectToPage($"/admin/events/{eventId}/invitees");
        }

        string? accommodationCode = null;
        if (invite.AllowUseOfAccommodationCode)
        {
            accommodationCode = await db.Events
                .Where(e => e.Id == eventId)
                .Select(e => e.AccommodationCode)
                .FirstOrDefaultAsync();
        }

        var newEventUsers = newUserIds.Select(userId => new EventUser
        {
            EventId = eventId,
            UserId = userId,
            AssignedAccommodationCode = accommodationCode,
            ScheduledFor = invite.ScheduledFor,
            InviteEmailSent = false
        }).ToList();

        db.EventUsers.AddRange(newEventUsers);

        var count = await db.SaveChangesAsync();

        // Only send emails for immediate invites (not scheduled ones)
        if (invite is { SendEmailsOnInvite: true, ScheduledFor: null })
        {
            var newInvitations = await db.EventUsers
                .Where(ue => ue.EventId == eventId && newUserIds.Contains(ue.UserId))
                .Select(ue => new Invitation
                {
                    EventId = ue.EventId,
                    UserId = ue.UserId,
                    EventName = ue.Event.Name,
                    UserDisplayName = ue.User.DisplayName!,
                    UserEmail = ue.User.Email!,
                    InviteCode = validCode
                })
                .ToListAsync();

            await invitationService.SendImmediateInvitationsAsync(newInvitations);
        }

        toastNotification.AddSuccessToastMessage($"Successfully created {count} invitations");
        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("{eventId:int}/resend-invite")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ResendInvite([FromRoute] int eventId, [FromForm] string userId, [FromForm] string inviteCode)
    {
        var eventUser = await db.EventUsers
            .Include(eu => eu.User)
            .Where(eu => eu.EventId == eventId && eu.UserId == userId)
            .Select(eu => new { eu.User.Email, eu.User.DisplayName })
            .FirstOrDefaultAsync();

        if (eventUser is null)
        {
            toastNotification.AddWarningToastMessage("User is not invited to this event");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var eventData = await db.Events
            .Where(e => e.Id == eventId)
            .Select(e => new { e.Id, e.Name, e.InviteCodes })
            .FirstOrDefaultAsync();

        if (!IsValidCode(eventData?.InviteCodes, inviteCode, out var validCode) || eventData is null)
        {
            toastNotification.AddWarningToastMessage("Invalid or expired invite code");
            return RedirectToPage($"/admin/events/{eventId}/invitees");
        }

        try
        {
            logger.LogInformation("Re-sending invitation to {Email}", eventUser.Email);
            await invitationService.SendInvitationAsync(new Invitation
            {
                EventId = eventData.Id,
                EventName = eventData.Name,
                UserDisplayName = eventUser.DisplayName!,
                UserEmail = eventUser.Email!,
                UserId = userId,
                InviteCode = validCode
            });

            await db.EventUsers
                .Where(eu => eu.EventId == eventId && eu.UserId == userId)
                .ExecuteUpdateAsync(setter => setter.SetProperty(eu => eu.InviteEmailSent, true));

            await db.SaveChangesAsync();

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
        var invitedEventId = await db.InviteCodes
            .Where(c => c.Code == code && c.ValidUntil > DateTime.UtcNow)
            .Select(ic => ic.EventId)
            .FirstOrDefaultAsync();

        if (invitedEventId <= 0)
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

        var isInvited = await db.EventUsers.AnyAsync(eu => eu.EventId == invitedEventId && eu.UserId == user.Id);
        if (!isInvited)
        {
            logger.LogWarning("User with email {Email} was not invited to event {EventId}", email, invitedEventId);
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
            Code = CodeGenerator.Generate(16),
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

    private static bool IsValidCode(HashSet<InviteCode>? inviteCodes, string? inviteCode, out string? validCode)
    {
        validCode = inviteCodes?
            .Where(c => c.Code == inviteCode && c.ValidUntil > DateTime.UtcNow)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => c.Code)
            .FirstOrDefault();

        return !string.IsNullOrWhiteSpace(validCode);
    }
}
