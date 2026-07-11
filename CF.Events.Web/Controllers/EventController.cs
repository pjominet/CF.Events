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
    IInvitationService invitationService,
    IExportService exportService,
    IToastNotification toastNotification,
    ILogger<EventController> logger,
    IWebHostEnvironment env) : Controller
{
    [HttpGet("{eventId:int}/export-invitees")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ExportInvitees([FromRoute] int eventId)
    {
        try
        {
            var (bytes, fileName) = await exportService.ExportInviteesToExcelAsync(eventId);
            Response.Cookies.Append("fileDownload", "true", new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Lax });
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (ArgumentException ex)
        {
            toastNotification.AddErrorToastMessage(ex.Message);
            return RedirectToPage("/Admin/Events");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting invitees for event {EventId}", eventId);
            toastNotification.AddErrorToastMessage("An error occurred while exporting invitees.");
            return RedirectToPage("/Admin/Events");
        }
    }

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
        if (userId is null) return Unauthorized();

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
            DonationReference = new Event { Name = eventUser.EventName, StartDate = eventUser.EventStartDate }.GetDonationReference()
        };

        return PartialView("~/Pages/Shared/_RsvpDetailsModal.cshtml", model);
    }

    [HttpGet("{eventId:int}/rsvp-responses/{userId}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetRsvpResponses([FromRoute] int eventId, [FromRoute] string userId)
    {
        var eventUser = await db.EventUsers
            .Include(eu => eu.Rsvp)
            .Where(eu => eu.EventId == eventId && eu.UserId == userId)
            .Select(eu => new
            {
                AttendanceDays = eu.Rsvp != null ? eu.Rsvp.AttendanceDays : new List<int>(),
                DietaryOptions = eu.Rsvp != null ? eu.Rsvp.CommonDietaryOptions : new List<DietaryOptions>(),
                OtherDietaryDetails = eu.Rsvp != null ? eu.Rsvp.OtherDietaryDetails : null,
                Comments = eu.Rsvp != null ? eu.Rsvp.Comments : null,
                UserDisplayName = eu.User.DisplayName
            })
            .FirstOrDefaultAsync();

        if (eventUser is null) return NotFound();

        var model = new RsvpResponses
        {
            AttendanceDays = eventUser.AttendanceDays,
            DietaryOptions = eventUser.DietaryOptions,
            OtherDietaryDetails = eventUser.OtherDietaryDetails,
            Comments = eventUser.Comments
        };

        ViewData["UserDisplayName"] = eventUser.UserDisplayName;

        return PartialView("~/Pages/Admin/Shared/_RsvpResponsesModal.cshtml", model);
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
                .Select(ue => new InvitationEmailRequest
                {
                    TemplateId = ue.Event.InvitationTemplateId ?? string.Empty,
                    EventId = ue.EventId,
                    UserId = ue.UserId,
                    EventName = ue.Event.Name,
                    UserName = ue.User.DisplayName!,
                    UserEmail = ue.User.Email!,
                    InviteCode = inviteCode,
                    CallBackUrl = (invitationService.AppSettings.BaseUrl ?? string.Empty).TrimEnd('/') + "/events/invite-callback?code=" + inviteCode + "&id=" + ue.UserId
                })
                .ToListAsync();

            await invitationService.SendImmediateEmails(newInvitations);
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
        var @event = await db.Events
            .Where(e => e.Id == eventId)
            .Select(e => new { e.Id, e.Name })
            .FirstOrDefaultAsync();

        if (@event is null)
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
            await invitationService.SendEmail(new InvitationEmailRequest
            {
                TemplateId = await db.Events.Where(e => e.Id == @event.Id).Select(e => e.InvitationTemplateId).FirstOrDefaultAsync() ?? string.Empty,
                EventId = @event.Id,
                EventName = @event.Name,
                UserName = eventUser.DisplayName!,
                UserEmail = eventUser.Email!,
                UserId = userId,
                InviteCode = inviteCode,
                CallBackUrl = (invitationService.AppSettings.BaseUrl ?? string.Empty).TrimEnd('/') + "/events/invite-callback?code=" + inviteCode + "&id=" + userId
            });

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
            .Select(e => new { e.Id, e.Name, e.InvitationTemplateId })
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

        var requests = new List<InvitationEmailRequest>();
        var baseUrl = (invitationService.AppSettings.BaseUrl ?? string.Empty).TrimEnd('/');

        foreach (var user in eventUsers)
        {
            if (user.InviteCodeId <= 0 || !inviteCodes.TryGetValue(user.InviteCodeId, out var code))
                continue;

            requests.Add(new InvitationEmailRequest
            {
                TemplateId = eventData.InvitationTemplateId ?? string.Empty,
                EventId = eventData.Id,
                EventName = eventData.Name,
                UserName = user.DisplayName!,
                UserEmail = user.Email!,
                UserId = user.UserId,
                InviteCode = code,
                CallBackUrl = $"{baseUrl}/events/invite-callback?code={code}&id={user.UserId}"
            });
        }

        if (requests.Count > 0)
        {
            await invitationService.SendImmediateEmails(requests);
            toastNotification.AddSuccessToastMessage($"Successfully resent {requests.Count} invitations");
        }
        else toastNotification.AddErrorToastMessage("No invitations could be sent. Check if invite codes are valid/expired.");

        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpGet("invite-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> InvitationCallback([FromQuery] string code, [FromQuery] string id)
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

        var user = await db.EventUsers
            .Where(eu => eu.EventId == invitedEventId && eu.UserId == id)
            .Select(eu => eu.User)
            .FirstOrDefaultAsync();

        if (user is null || !user.IsActive)
        {
            logger.LogWarning("User with id {Id} not found or inactive", id);
            return BadRequest();
        }

        if (User.Identity?.IsAuthenticated == true && User.Identity?.Name == user.Email
            || string.IsNullOrWhiteSpace(user.PasswordHash) && !user.MustChangePassword)
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
