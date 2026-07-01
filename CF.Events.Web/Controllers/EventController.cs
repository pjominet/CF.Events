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
        var isInvited = await db.InvitedPersons
            .AnyAsync(ip => ip.Invitation.EventId == eventId && ip.UserId == userId);
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
    public async Task<IActionResult> InviteUsers([FromRoute] int eventId, [FromForm] InviteUsersRequest invite)
    {
        if (invite.ScheduledFor.HasValue && invite.ScheduledFor.Value.ToUniversalTime() <= DateTime.UtcNow)
        {
            toastNotification.AddWarningToastMessage("Scheduled time must be in the future");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        // Validate event exists and is active
        var eventExists = await db.Events.AnyAsync(e => e.Id == eventId && e.IsActive);
        if (!eventExists)
        {
            toastNotification.AddWarningToastMessage("Event not found or not active anymore");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        // Get users who are already invited to this event (via InvitedPersons)
        var existingUserIds = await db.InvitedPersons
            .Where(ip => ip.Invitation.EventId == eventId)
            .Select(ip => ip.UserId)
            .Where(userId => userId != null)
            .ToListAsync();

        // Filter to only new users (not already invited)
        var newUserIds = invite.UserIds
            .Where(userId => !existingUserIds.Contains(userId))
            .ToList();

        if (newUserIds.Count == 0)
        {
            toastNotification.AddWarningToastMessage("All selected users are already invited to this event");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        string? accommodationCode = null;
        if (invite.AllowUseOfAccommodationCode)
        {
            accommodationCode = await db.Events
                .Where(e => e.Id == eventId)
                .Select(e => e.AccommodationCode)
                .FirstOrDefaultAsync();
        }

        // Create a new invitation for this group of users
        var newInvitation = new Invitation
        {
            EventId = eventId,
            InviteCodeId = invite.InviteCodeId,
            ScheduledFor = invite.ScheduledFor,
            InviteEmailSent = false,
            AssignedAccommodationCode = accommodationCode,
            Status = InvitationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            GroupName = "Group Invitation"
        };

        // Add all new users as invited persons to this invitation
        var newInvitedPersons = newUserIds.Select((userId, index) => new InvitedPerson
        {
            Invitation = newInvitation,
            UserId = userId,
            IsPrimary = index == 0, // First user is primary
            Status = PersonInviteStatus.Pending
        }).ToList();

        db.Invitations.Add(newInvitation);
        db.InvitedPersons.AddRange(newInvitedPersons);

        var count = await db.SaveChangesAsync();

        // Only send emails for immediate invites (not scheduled ones)
        if (invite is { SendEmailsOnInvite: true, ScheduledFor: null })
        {
            var inviteCode = db.InviteCodes
                .Where(c => c.EventId == eventId && c.Id == invite.InviteCodeId && c.ValidUntil > DateTime.UtcNow)
                .Select(c => c.Code)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                toastNotification.AddWarningToastMessage("Cannot find valid invite code for event");
                return LocalRedirect($"/admin/events/{eventId}/invitees");
            }

            // Get the newly created invitation and its primary person for sending email
            var createdInvitation = await db.Invitations
                .Where(i => i.EventId == eventId && i.InviteCodeId == invite.InviteCodeId && !i.InviteEmailSent)
                .OrderByDescending(i => i.CreatedAt)
                .Include(i => i.Event)
                .Include(i => i.InvitedPersons)
                    .ThenInclude(ip => ip.User)
                .FirstOrDefaultAsync();

            var primaryPerson = createdInvitation?.InvitedPersons.FirstOrDefault(ip => ip.IsPrimary);
            if (primaryPerson is { User: not null } && createdInvitation is not null)
            {
                var inviteRequest = invitationService.CreateInviteEmailRequest(createdInvitation, primaryPerson, inviteCode);
                await invitationService.SendImmediateInvitationsAsync([inviteRequest]);
            }
        }

        toastNotification.AddSuccessToastMessage($"Successfully created {count} invitations");
        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("{eventId:int}/resend-invite")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ResendInvite([FromRoute] int eventId, [FromForm] string userId)
    {
        var invitedPerson = await db.InvitedPersons
            .Include(ip => ip.User)
            .Include(ip => ip.Invitation)
                .ThenInclude(i => i.InviteCode)
            .Where(ip => ip.Invitation.EventId == eventId && ip.UserId == userId)
            .Select(ip => new {
                ip.User.Email,
                ip.User.DisplayName,
                ip.Name,
                InviteCodeId = ip.Invitation.InviteCodeId,
                InvitationId = ip.InvitationId
            })
            .FirstOrDefaultAsync();

        if (invitedPerson is null)
        {
            toastNotification.AddWarningToastMessage("User is not invited to this event");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        // get invite codes
        var eventData = await db.Events
            .Where(e => e.Id == eventId)
            .Select(e => new { e.Id, e.Name })
            .FirstOrDefaultAsync();

        if (eventData is null)
        {
            toastNotification.AddWarningToastMessage("Event does not exist");
            return RedirectToPage($"/admin/events/{eventId}/invitees");
        }

        var inviteCode = db.InviteCodes
            .Where(c => c.EventId == eventId && c.Id == invitedPerson.InviteCodeId && c.ValidUntil > DateTime.UtcNow)
            .Select(c => c.Code)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(inviteCode))
        {
            toastNotification.AddWarningToastMessage("Invalid or expired invite code");
            return RedirectToPage($"/admin/events/{eventId}/invitees");
        }

        try
        {
            // Get the full invitation and invited person to use the service method
            var fullInvitedPerson = await db.InvitedPersons
                .Include(ip => ip.Invitation)
                    .ThenInclude(i => i.Event)
                .Include(ip => ip.User)
                .FirstOrDefaultAsync(ip => ip.InvitationId == invitedPerson.InvitationId && ip.UserId == userId);

            if (fullInvitedPerson?.Invitation is null)
            {
                toastNotification.AddWarningToastMessage("Could not load invitation details");
                return RedirectToPage($"/admin/events/{eventId}/invitees");
            }

            logger.LogInformation("Re-sending invitation to {Email}", invitedPerson.Email);
            var inviteRequest = invitationService.CreateInviteEmailRequest(
                fullInvitedPerson.Invitation,
                fullInvitedPerson,
                inviteCode);
            await invitationService.SendInvitationAsync(inviteRequest);

            await db.Invitations
                .Where(i => i.Id == invitedPerson.InvitationId)
                .ExecuteUpdateAsync(setter => setter.SetProperty(i => i.InviteEmailSent, true));

            await db.SaveChangesAsync();

            toastNotification.AddSuccessToastMessage("Successfully resent invitation");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }
        catch
        {
            toastNotification.AddErrorToastMessage("Invitation could not be resent");
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

        var isInvited = await db.InvitedPersons
            .AnyAsync(ip => ip.Invitation.EventId == invitedEventId && ip.UserId == user.Id);
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
    public async Task<IActionResult> RegenerateCode([FromRoute] int eventId, [FromForm] int validDays, [FromForm] string? label)
    {
        // Try to use referrer for redirect if possible, otherwise default to events list
        var referrer = Request.Headers.Referer.ToString();
        var @event = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (@event is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            if (!string.IsNullOrEmpty(referrer))
                return Redirect(referrer);
            return RedirectToPage("/admin/events");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            var firstWord = @event.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "EVENT";
            label = $"{firstWord.ToUpper()}{@event.Date.Year}";
        }

        var newCode = new InviteCode
        {
            EventId = eventId,
            Code = CodeGenerator.Generate(32),
            Label = label,
            ValidUntil = DateTime.UtcNow.AddDays(validDays),
            CreatedAt = DateTime.UtcNow
        };

        db.InviteCodes.Add(newCode);
        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage($"New invite code '{label}' generated");

        if (!string.IsNullOrEmpty(referrer))
            return Redirect(referrer);
        return RedirectToPage("/admin/events");
    }
}
