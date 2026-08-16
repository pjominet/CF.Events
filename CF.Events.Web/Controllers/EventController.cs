using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
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
    [HttpGet("{eventId:int}/{userId}/asset")]
    public async Task<IActionResult> GetEventAsset([FromRoute] int eventId, [FromRoute] string userId, [FromQuery] string type)
    {
        var isAdmin = User.IsAdmin();
        if (User.GetId() != userId && !isAdmin)
            return Forbid();

        var isInvited = await db.EventUsers.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
        if (!isInvited && !isAdmin)
            return Forbid();

        var resourceRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Resources"));
        var filePath = type == "sd" ? "Assets/save-the-date.png" : null;
        if (filePath is null)
            return NotFound();

        var requested = Path.GetFullPath(Path.Combine(resourceRoot, filePath));
        if (!requested.StartsWith(resourceRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return Forbid();

        if (!System.IO.File.Exists(requested))
            return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(requested, out var contentType))
            contentType = "application/octet-stream";

        return PhysicalFile(requested, contentType);
    }

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

    [HttpGet("{eventId:int}/accommodation")]
    [Authorize]
    public async Task<IActionResult> GetEventAccommodationDetail([FromRoute] int eventId)
    {
        var userId = User.GetId();

        var eventUser = await db.EventUsers
            .Where(eu => eu.EventId == eventId && eu.UserId == userId)
            .Select(eu => new
            {
                HasRsvped = eu.Rsvp != null && eu.Rsvp.SubmittedAt <= DateTime.UtcNow,
                IsAttending = eu.Rsvp != null && eu.Rsvp.Attending,
                eu.Event.AccommodationDetails,
                AccommodationCode = eu.AssignedAccommodationCode,
                BookingLinks = eu.Event.BookingLinks.Select(bl => new { bl.Type, bl.Link }).ToList()
            })
            .FirstOrDefaultAsync();

        if (eventUser is null) return NotFound();

        var model = new AccommodationDetails
        {
            HasRsvped = eventUser.HasRsvped,
            IsAttending = eventUser.IsAttending,
            Details = eventUser.AccommodationDetails,
            Code = eventUser.AccommodationCode,
            BookingLinks = eventUser.BookingLinks.ToDictionary(bl => bl.Type, bl => bl.Link)
        };

        return PartialView("~/Pages/Events/Shared/_EventAccommodation.cshtml", model);
    }

    [HttpGet("{eventId:int}/donations")]
    [Authorize]
    public async Task<IActionResult> GetEventDonationDetail([FromRoute] int eventId)
    {
        var userId = User.GetId();

        var eventUser = await db.EventUsers
            .Where(eu => eu.EventId == eventId && eu.UserId == userId)
            .Select(eu => new
            {
                EventName = eu.Event.Name,
                eu.Event.DonationIban,
                eu.Event.DonationLink,
                EventStartDate = eu.Event.StartDate
            })
            .FirstOrDefaultAsync();

        if (eventUser is null) return NotFound();

        var model = new DonationDetails
        {
            Iban = eventUser.DonationIban,
            Link = eventUser.DonationLink,
            Reference = new Event { Name = eventUser.EventName, StartDate = eventUser.EventStartDate }.GetDonationReference()
        };

        return PartialView("~/Pages/Events/Shared/_EventDonations.cshtml", model);
    }

    [HttpGet("{eventId:int}/faq")]
    [Authorize]
    public async Task<IActionResult> GetEventFaq([FromRoute] int eventId)
    {
        var userId = User.GetId();

        var eventUser = await db.EventUsers
            .Where(eu => eu.EventId == eventId && eu.UserId == userId)
            .Select(eu => new
            {
                eu.Event.EventFaq
            })
            .FirstOrDefaultAsync();

        if (eventUser is null) return NotFound();

        return PartialView("~/Pages/Events/Shared/_EventFaq.cshtml", eventUser.EventFaq);
    }

    [HttpGet("{eventId:int}/schedule")]
    [Authorize]
    public async Task<IActionResult> GetEventSchedule([FromRoute] int eventId)
    {
        var userId = User.GetId();

        var eventUser = await db.EventUsers
            .Where(eu => eu.EventId == eventId && eu.UserId == userId)
            .Select(eu => new
            {
                eu.Event.EventSchedule
            })
            .FirstOrDefaultAsync();

        if (eventUser is null) return NotFound();

        return PartialView("~/Pages/Events/Shared/_EventSchedule.cshtml", eventUser.EventSchedule);
    }

    [HttpGet("{eventId:int}/travel")]
    [Authorize]
    public async Task<IActionResult> GetEventTravelInstructions([FromRoute] int eventId)
    {
        var userId = User.GetId();

        var eventUser = await db.EventUsers
            .Where(eu => eu.EventId == eventId && eu.UserId == userId)
            .Select(eu => new
            {
                eu.Event.TravelInstructions
            })
            .FirstOrDefaultAsync();

        if (eventUser is null) return NotFound();

        return PartialView("~/Pages/Events/Shared/_EventTravel.cshtml", eventUser.TravelInstructions);
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

        var guestGroup = await db.GuestGroups.FirstOrDefaultAsync(gg => gg.GuestUserId == userId);

        var model = new RsvpResponses
        {
            GuestGroup = guestGroup?.Label ?? "Guest Group",
            ParticipantsAttendance = rsvp.ParticipantsAttendance,
            ParticipantsDiets = rsvp.ParticipantsDiets,
            Comments = rsvp.Comments
        };

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
}
