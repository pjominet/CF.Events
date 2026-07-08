using System.Security.Claims;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
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

    [HttpGet("{eventId:int}/rsvp-detail")]
    [Authorize]
    public async Task<IActionResult> GetRsvpDetail([FromRoute] int eventId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var eventUser = await db.EventUsers
            .Where(eu => eu.EventId == eventId && eu.UserId == userId)
            .Select(eu => new
            {
                HasRsvped = eu.Rsvp != null && eu.Rsvp.SubmittedAt <= DateTime.UtcNow,
                IsAttending = eu.Rsvp != null && eu.Rsvp.Attending,
                EventName = eu.Event.Name,
                eu.Event.AccommodationDetails,
                AccommodationCode = eu.AssignedAccommodationCode,
                BookingLinks = eu.Event.BookingLinks.Select(bl => new { bl.Type, bl.Link }).ToList(),
                eu.Event.DonationIban,
                EventStartDate = eu.Event.StartDate,
                AttendanceDays = eu.Rsvp != null ? eu.Rsvp.AttendanceDays : new List<int>()
            })
            .FirstOrDefaultAsync();

        if (eventUser is null) return NotFound();

        var model = new RsvpDetail
        {
            HasRsvped = eventUser.HasRsvped,
            IsAttending = eventUser.IsAttending,
            EventName = eventUser.EventName,
            AccommodationDetails = eventUser.AccommodationDetails,
            AccommodationCode = eventUser.AccommodationCode,
            BookingLinks = eventUser.BookingLinks.ToDictionary(bl => bl.Type, bl => bl.Link),
            DonationIban = eventUser.DonationIban,
            DonationReference = new Event { Name = eventUser.EventName, StartDate = eventUser.EventStartDate }.GetDonationReference(),
            AttendanceDays = eventUser.AttendanceDays
        };

        return PartialView("_RsvpDetails", model);
    }

    [HttpPost("{eventId:int}/invite-users")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> InviteUsers([FromRoute] int eventId, [FromForm] UsersInviteRequest inviteRequest)
    {
        if (inviteRequest.ScheduledFor.HasValue && inviteRequest.ScheduledFor.Value.ToUniversalTime() <= DateTime.UtcNow)
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

        // Get users who are already invited to this event
        var existingUserIds = await db.EventUsers
            .Where(ue => ue.EventId == eventId)
            .Select(ue => ue.UserId)
            .ToListAsync();

        // Filter to only new users (not already invited)
        var newUserIds = inviteRequest.UserIds
            .Where(userId => !existingUserIds.Contains(userId))
            .ToList();

        if (newUserIds.Count == 0)
        {
            toastNotification.AddWarningToastMessage("All selected users are already invited to this event");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        if (inviteRequest.AllowAccommodationCode)
        {
            var isValidCode = await db.Events
                .Where(e => e.Id == eventId)
                .AnyAsync(e => e.AccommodationCodes.Any(c => c == inviteRequest.SelectedAccommodationCode));

            if (!isValidCode)
            {
                toastNotification.AddWarningToastMessage("Selected accommodation code does not exist or is invalid");
                return LocalRedirect($"/admin/events/{eventId}/invitees");
            }
        }

        var newEventUsers = newUserIds.Select(userId => new EventUser
        {
            EventId = eventId,
            UserId = userId,
            AssignedAccommodationCode = inviteRequest.AllowAccommodationCode ? inviteRequest.SelectedAccommodationCode : null,
            ScheduledFor = inviteRequest.ScheduledFor,
            InviteEmailSent = false,
            InviteCodeId = inviteRequest.InviteCodeId
        }).ToList();

        db.EventUsers.AddRange(newEventUsers);

        var count = await db.SaveChangesAsync();

        // Only send emails for immediate invites (not scheduled ones)
        if (inviteRequest is { SendEmailsOnInvite: SendEmailAction.Immediately, ScheduledFor: null })
        {
            var inviteCode = db.InviteCodes
                .Where(c => c.EventId == eventId && c.Id == inviteRequest.InviteCodeId && c.ValidUntil > DateTime.UtcNow)
                .Select(c => c.Code)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                toastNotification.AddWarningToastMessage("Cannot find valid invite code for event");
                return LocalRedirect($"/admin/events/{eventId}/invitees");
            }

            var newInvitations = await db.EventUsers
                .Where(ue => ue.EventId == eventId && newUserIds.Contains(ue.UserId))
                .Select(ue => new InviteEmailRequest
                {
                    EventId = ue.EventId,
                    UserId = ue.UserId,
                    EventName = ue.Event.Name,
                    UserDisplayName = ue.User.DisplayName!,
                    UserEmail = ue.User.Email!,
                    InviteCode = inviteCode
                })
                .ToListAsync();

            await invitationService.SendImmediateInvitationsAsync(newInvitations);
        }

        toastNotification.AddSuccessToastMessage($"Successfully created {count} invitations");
        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("{eventId:int}/resend-invite")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ResendInvite([FromRoute] int eventId, [FromForm] string userId)
    {
        var eventUser = await db.EventUsers
            .Include(eu => eu.User)
            .Where(eu => eu.EventId == eventId && eu.UserId == userId)
            .Select(eu => new { eu.User.Email, eu.User.DisplayName, eu.InviteCodeId })
            .FirstOrDefaultAsync();

        if (eventUser is null)
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
            .Where(c => c.EventId == eventId && c.Id == eventUser.InviteCodeId && c.ValidUntil > DateTime.UtcNow)
            .Select(c => c.Code)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(inviteCode))
        {
            toastNotification.AddWarningToastMessage("Invalid or expired invite code");
            return RedirectToPage($"/admin/events/{eventId}/invitees");
        }

        try
        {
            logger.LogInformation("Re-sending invitation to {Email}", eventUser.Email);
            await invitationService.SendInvitationAsync(new InviteEmailRequest
            {
                EventId = eventData.Id,
                EventName = eventData.Name,
                UserDisplayName = eventUser.DisplayName!,
                UserEmail = eventUser.Email!,
                UserId = userId,
                InviteCode = inviteCode
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

    [HttpPost("{eventId:int}/resend-invites")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> BulkResendInvite([FromRoute] int eventId, [FromForm] string userIds)
    {
        if (string.IsNullOrWhiteSpace(userIds))
        {
            toastNotification.AddWarningToastMessage("No users selected");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var ids = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var eventUsers = await db.EventUsers
            .Include(eu => eu.User)
            .Where(eu => eu.EventId == eventId && ids.Contains(eu.UserId))
            .Select(eu => new { eu.UserId, eu.User.Email, eu.User.DisplayName, eu.InviteCodeId })
            .ToListAsync();

        if (eventUsers.Count == 0)
        {
            toastNotification.AddWarningToastMessage("No users found to resend invitations");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var eventData = await db.Events
            .Where(e => e.Id == eventId)
            .Select(e => new { e.Id, e.Name })
            .FirstOrDefaultAsync();

        if (eventData is null)
        {
            toastNotification.AddWarningToastMessage("Event does not exist");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var inviteCodeIds = eventUsers.Select(eu => eu.InviteCodeId).Distinct().ToList();
        var inviteCodes = await db.InviteCodes
            .Where(c => c.EventId == eventId && inviteCodeIds.Contains(c.Id) && c.ValidUntil > DateTime.UtcNow)
            .ToDictionaryAsync(c => c.Id, c => c.Code);

        var successCount = 0;
        foreach (var user in eventUsers)
        {
            if (user.InviteCodeId <= 0 || !inviteCodes.TryGetValue(user.InviteCodeId, out var code))
                continue;
            try
            {
                await invitationService.SendInvitationAsync(new InviteEmailRequest
                {
                    EventId = eventData.Id,
                    EventName = eventData.Name,
                    UserDisplayName = user.DisplayName!,
                    UserEmail = user.Email!,
                    UserId = user.UserId,
                    InviteCode = code
                });
                successCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to resend invitation to {Email}", user.Email);
            }
        }

        if (successCount > 0)
        {
            var sentIds = eventUsers
                .Where(u => u.InviteCodeId > 0 && inviteCodes.ContainsKey(u.InviteCodeId))
                .Select(u => u.UserId)
                .ToList();

            await db.EventUsers
                .Where(eu => eu.EventId == eventId && sentIds.Contains(eu.UserId))
                .ExecuteUpdateAsync(setter => setter.SetProperty(eu => eu.InviteEmailSent, true));

            toastNotification.AddSuccessToastMessage($"Successfully resent {successCount} invitations");
        }
        else toastNotification.AddErrorToastMessage("No invitations could be sent. Check if invite codes are valid/expired.");

        return LocalRedirect($"/admin/events/{eventId}/invitees");
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
            label = $"{firstWord.ToUpper()}{@event.StartDate.Year}";
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
