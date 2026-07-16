using CF.Events.Web.Data;
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
    SignInManager<AppUser> signInManager,
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

    [HttpGet("{folderName}/image/{fileName}")]
    public async Task<IActionResult> GetEventImage([FromRoute] string folderName, [FromRoute] string fileName)
    {
        var userId = User.GetId();
        bool isInvited;

        if (int.TryParse(folderName, out var eventId))
            isInvited = await db.EventUsers.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
        else isInvited = false;

        if (!isInvited && !User.IsAdmin())
            return Forbid();

        var eventsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Resources", "Events"));
        var requested = Path.GetFullPath(Path.Combine(eventsRoot, folderName, fileName));

        if (!requested.StartsWith(eventsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
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
        var userId = User.GetId();

        var eventUser = await db.EventUsers
            .Where(eu => eu.EventId == eventId && eu.UserId == userId)
            .Select(eu => new
            {
                HasRsvped = eu.Rsvp != null && eu.Rsvp.SubmittedAt <= DateTime.UtcNow,
                IsAttending = eu.Rsvp != null && eu.Rsvp.Attending,
                EventName = eu.Event.Name,
                eu.Event.AccommodationDetails,
                AccommodationCode = eu.AssignedAccommodationCode,
                ParticipantAttendance = eu.Rsvp != null ? eu.Rsvp.ParticipantsAttendance : new List<ParticipantAttendance>(),
                DietaryOptions = eu.Rsvp != null ? eu.Rsvp.ParticipantsDiets : new List<ParticipantDiet>(),
                BookingLinks = eu.Event.BookingLinks.Select(bl => new { bl.Type, bl.Link }).ToList(),
                eu.Event.DonationIban,
                eu.Event.DonationLink,
                EventStartDate = eu.Event.StartDate
            })
            .FirstOrDefaultAsync();

        if (eventUser is null) return NotFound();

        var model = new RsvpDetail
        {
            HasRsvped = eventUser.HasRsvped,
            IsAttending = eventUser.IsAttending,
            EventName = eventUser.EventName,
            ParticipantsAttendance = eventUser.ParticipantAttendance,
            AccommodationDetails = eventUser.AccommodationDetails,
            AccommodationCode = eventUser.AccommodationCode,
            ParticipantsDiets = eventUser.DietaryOptions,
            BookingLinks = eventUser.BookingLinks.ToDictionary(bl => bl.Type, bl => bl.Link),
            DonationIban = eventUser.DonationIban,
            DonationLink = eventUser.DonationLink,
            DonationReference = new Event { Name = eventUser.EventName, StartDate = eventUser.EventStartDate }.GetDonationReference()
        };

        return PartialView("~/Pages/Shared/_RsvpDetailsModal.cshtml", model);
    }

    [HttpGet("{eventId:int}/rsvp-responses/{userId}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetRsvpResponses([FromRoute] int eventId, [FromRoute] string userId)
    {
        var rsvp = await db.Rsvps
            .Where(r => r.EventId == eventId && r.UserId == userId)
            .Select(r => new
            {
                r.ParticipantsAttendance,
                r.ParticipantsDiets,
                r.Comments
            })
            .FirstOrDefaultAsync();

        if (rsvp is null) return NotFound();

        var model = new RsvpResponses
        {
            ParticipantsAttendance = rsvp.ParticipantsAttendance,
            ParticipantsDiets = rsvp.ParticipantsDiets,
            Comments = rsvp.Comments
        };

        ViewData[ViewDataKeys.GuestGroupLabel] = await db.GuestGroups.FirstOrDefaultAsync(gg => gg.GuestUserId == userId);

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

        try
        {
            var count = await invitationService.InviteUsersAsync(eventId, inviteRequest);

            if (count == 0)
                toastNotification.AddWarningToastMessage("All selected users are already invited to this event");
            else toastNotification.AddSuccessToastMessage($"Successfully created {count} invitations");

            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }
        catch (ArgumentException ex)
        {
            toastNotification.AddWarningToastMessage(ex.Message);
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inviting users for event {EventId}", eventId);
            toastNotification.AddErrorToastMessage("An error occurred while inviting users.");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }
    }

    [HttpPost("{eventId:int}/resend-invite")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ResendInvite([FromRoute] int eventId, [FromForm] string userId)
    {
        try
        {
            await invitationService.ResendInvitesAsync(eventId, [userId]);
            toastNotification.AddSuccessToastMessage("Successfully resent invitation");
        }
        catch (ArgumentException ex)
        {
            toastNotification.AddWarningToastMessage(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resending invitation to {UserId} for event {EventId}", userId, eventId);
            toastNotification.AddErrorToastMessage("Invitation could not be resent");
        }

        return LocalRedirect($"/admin/events/{eventId}/invitees");
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

        try
        {
            await invitationService.ResendInvitesAsync(eventId, ids);
            toastNotification.AddSuccessToastMessage($"Successfully resent {ids.Count} invitations");
        }
        catch (ArgumentException ex)
        {
            toastNotification.AddWarningToastMessage(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk resending invitations for event {EventId}", eventId);
            toastNotification.AddErrorToastMessage("Invitations could not be resent");
        }

        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpGet("invite-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> InvitationCallback([FromQuery] string code, [FromQuery] int? eventId)
    {
        var invitedUser = await db.InviteCodes
            .Where(c => c.Value == code && c.ValidUntil > DateTime.UtcNow)
            .Select(ic => ic.User)
            .FirstOrDefaultAsync();

        if (invitedUser is null)
        {
            logger.LogWarning("Invalid or expired invite code was used: {Code}", code);
            return BadRequest();
        }

        if (!invitedUser.IsActive)
        {
            logger.LogWarning("User with id {Id} is inactive", invitedUser.Id);
            return BadRequest();
        }

        await signInManager.SignInAsync(invitedUser, false);

        return LocalRedirect(eventId.HasValue ? $"/events/{eventId}/invitation" : "/");
    }
}
