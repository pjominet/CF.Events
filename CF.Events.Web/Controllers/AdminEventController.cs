using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[Route("admin/events/{eventId:int}")]
[Authorize(Roles = Roles.Admin)]
public class AdminEventController(
    EventsDbContext db,
    IInvitationService invitationService,
    IExportService exportService,
    IToastNotification toastNotification,
    ILogger<AdminEventController> logger) : Controller
{
    [HttpGet("export-invitees")]
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

    [HttpGet("rsvp-responses/{userId}")]
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

    [HttpPost("invite-users")]
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

    [HttpPost("resend-invite")]
    public async Task<IActionResult> SendInvite([FromRoute] int eventId, [FromForm] string userId)
    {
        try
        {
            await invitationService.SendInvitesAsync(eventId, [userId]);
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

    [HttpPost("resend-invites")]
    public async Task<IActionResult> SendInvites([FromRoute] int eventId, [FromForm] string userIds)
    {
        if (!userIds.HasValue())
        {
            toastNotification.AddWarningToastMessage("No users selected");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var ids = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        try
        {
            await invitationService.SendInvitesAsync(eventId, ids);
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

    [HttpPost("remove-invitee")]
    public async Task<IActionResult> RemoveInvitee([FromRoute] int eventId, [FromForm] string userId)
    {
        var userEvent = await db.EventUsers.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (userEvent is null)
        {
            toastNotification.AddWarningToastMessage("Invitee not found");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        db.EventUsers.Remove(userEvent);
        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Invitee successfully removed");
        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("remove-invitees")]
    public async Task<IActionResult> RemoveInvitees([FromRoute] int eventId, [FromForm] string userIds)
    {
        if (!userIds.HasValue())
        {
            toastNotification.AddWarningToastMessage("No users selected");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var ids = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var userEvents = await db.EventUsers
            .Where(r => r.EventId == eventId && ids.Contains(r.UserId))
            .ToListAsync();

        if (userEvents.Count == 0)
        {
            toastNotification.AddWarningToastMessage("No invitees found to remove");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        db.EventUsers.RemoveRange(userEvents);
        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage($"Successfully removed {userEvents.Count} invitees");
        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("send-save-the-date")]
    public async Task<IActionResult> SendSaveTheDate([FromRoute] int eventId, [FromForm] string userId)
    {
        var result = await invitationService.SendSaveTheDateAsync(eventId, userId);

        switch (result.Status)
        {
            case EmailSendResultStatus.Success:
                toastNotification.AddSuccessToastMessage($"Save the Date email sent to {result.Message}");
                break;
            case EmailSendResultStatus.EventNotFound:
                toastNotification.AddErrorToastMessage(result.Message!);
                break;
            case EmailSendResultStatus.TemplateMissing:
            case EmailSendResultStatus.UserNotFound:
            case EmailSendResultStatus.Failed:
            default:
                toastNotification.AddWarningToastMessage(result.Message!);
                break;
        }

        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("send-save-the-dates")]
    public async Task<IActionResult> SendSaveTheDates([FromRoute] int eventId, [FromForm] string userIds)
    {
        if (!userIds.HasValue())
        {
            toastNotification.AddWarningToastMessage("No users selected");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var ids = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var result = await invitationService.SendBulkSaveTheDateAsync(eventId, ids);

        switch (result.Status)
        {
            case EmailSendResultStatus.Success:
                toastNotification.AddSuccessToastMessage($"Successfully sent {result.SentCount} Save the Date emails");
                break;
            case EmailSendResultStatus.TemplateMissing:
            case EmailSendResultStatus.UserNotFound:
            case EmailSendResultStatus.EventNotFound:
            case EmailSendResultStatus.Failed:
            default:
                toastNotification.AddErrorToastMessage(result.Message!);
                break;
        }

        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpGet("admin-rsvp/{userId}")]
    public async Task<IActionResult> GetAdminRsvpForm([FromRoute] int eventId, [FromRoute] string userId)
    {
        var user = await db.Users.Include(u => u.GuestGroup).FirstAsync(u => u.Id == userId);
        var participants = user.GuestGroup?.Participants ?? (user.DisplayName.HasValue() ? [user.DisplayName] : []);

        var rsvp = await db.Rsvps
            .Include(r => r.ParticipantsDiets)
            .Include(r => r.ParticipantsAttendance)
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

        var eventData = await db.Events
            .Where(e => e.Id == eventId)
            .Select(e => new { e.EventDuration, e.MaxParticipantsPerRsvp })
            .FirstAsync();

        var maxParticipants = user.GuestGroup?.MaxPeople ?? 0;

        if (maxParticipants == 0)
            maxParticipants = eventData.MaxParticipantsPerRsvp > 0 ? eventData.MaxParticipantsPerRsvp : 2;

        var model = new RsvpRequest
        {
            Participants = participants,
            Attending = rsvp?.Attending ?? true,
            ParticipantsAttendance = rsvp?.ParticipantsAttendance ?? [],
            ParticipantsDiets = rsvp?.ParticipantsDiets ?? [],
            Comments = rsvp?.Comments
        };

        return PartialView("~/Pages/Admin/Shared/_AdminRsvpForm.cshtml", (eventId, maxParticipants, eventData.EventDuration, userId, model));
    }

    [HttpPost("admin-rsvp/{userId}")]
    public async Task<IActionResult> AdminRsvp([FromRoute] int eventId, [FromRoute] string userId, [FromForm] RsvpRequest newRsvp)
    {
        var @event = await db.Events.FindAsync(eventId);
        if (@event is null)
        {
            toastNotification.AddErrorToastMessage("Event not found");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var user = await db.Users
            .Include(u => u.GuestGroup)
            .FirstAsync(u => u.Id == userId);

        var eventMaxParticipants = await db.Events
            .Where(e => e.Id == eventId)
            .Select(e => e.MaxParticipantsPerRsvp)
            .FirstAsync();

        var maxParticipants = user.GuestGroup?.MaxPeople ?? 0;

        if (maxParticipants == 0)
            maxParticipants = eventMaxParticipants > 0 ? eventMaxParticipants : 2;

        // remove empty entries to avoid false counts and saving of useless values in GuestGroup.Participants
        newRsvp.Participants = newRsvp.Participants.Where(p => p.HasValue()).ToList();

        if (newRsvp.Attending && newRsvp.Participants.Count > maxParticipants)
        {
            toastNotification.AddErrorToastMessage($"Maximum {maxParticipants} participants allowed per RSVP.");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        if (user.GuestGroup is not null)
        {
            user.GuestGroup.Participants = newRsvp.Participants;
            db.GuestGroups.Update(user.GuestGroup);
        }

        var rsvp = await db.Rsvps
            .Include(r => r.ParticipantsDiets)
            .Include(r => r.ParticipantsAttendance)
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

        if (rsvp is null)
        {
            rsvp = new Rsvp { EventId = eventId, UserId = userId };
            db.Rsvps.Add(rsvp);
        }

        rsvp.Attending = newRsvp.Attending;
        rsvp.SubmittedAt = DateTime.UtcNow;

        if (newRsvp.Attending)
        {
            // Handle attendance update
            var attendanceToDelete = await db.ParticipantsAttendance.Where(pa => pa.EventId == eventId && pa.UserId == userId).ToListAsync();
            db.ParticipantsAttendance.RemoveRange(attendanceToDelete);

            rsvp.ParticipantsAttendance =
            [
                .. newRsvp.ParticipantsAttendance.Select(pa => new ParticipantAttendance
                {
                    EventId = eventId,
                    UserId = userId,
                    ParticipantName = pa.ParticipantName,
                    AttendingDays = pa.AttendingDays
                })
            ];

            // Handle dietary options update
            var dietsToDelete = await db.ParticipantsDiets.Where(pd => pd.EventId == eventId && pd.UserId == userId).ToListAsync();
            db.ParticipantsDiets.RemoveRange(dietsToDelete);

            rsvp.ParticipantsDiets =
            [
                .. newRsvp.ParticipantsDiets.Select(o => new ParticipantDiet
                {
                    EventId = eventId,
                    UserId = userId,
                    ParticipantName = o.ParticipantName,
                    Restrictions = o.Restrictions,
                    OtherDetails = o.OtherDetails
                })
            ];

            rsvp.Comments = newRsvp.Comments;
        }
        else
        {
            var attendanceToDelete = await db.ParticipantsAttendance.Where(pa => pa.EventId == eventId && pa.UserId == userId).ToListAsync();
            db.ParticipantsAttendance.RemoveRange(attendanceToDelete);
            rsvp.ParticipantsAttendance = [];

            var dietsToDelete = await db.ParticipantsDiets.Where(pd => pd.EventId == eventId && pd.UserId == userId).ToListAsync();
            db.ParticipantsDiets.RemoveRange(dietsToDelete);
            rsvp.ParticipantsDiets = [];
        }

        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage($"RSVP updated for guest {userId}");
        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("update-invitees")]
    public async Task<IActionResult> UpdateInvitees([FromRoute] int eventId, [FromBody] List<InviteeUpdateRequest> updates)
    {
        if (updates.Count == 0)
            return Ok(new { count = 0 });

        var userIds = updates
            .Select(u => u.UserId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
            return Ok(new { count = 0 });

        var eventUsers = await db.EventUsers
            .Where(r => r.EventId == eventId && userIds.Contains(r.UserId))
            .ToListAsync();

        var userUpdatesGroups = updates
            .Where(u => !string.IsNullOrEmpty(u.UserId))
            .GroupBy(u => u.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var eventUser in eventUsers)
        {
            if (!userUpdatesGroups.TryGetValue(eventUser.UserId, out var userUpdates))
                continue;

            foreach (var update in userUpdates)
            {
                if (update.AccommodationCode.HasValue())
                    eventUser.AssignedAccommodationCode = update.AccommodationCode;

                if (update.Priority.HasValue)
                    eventUser.InvitationPriority = update.Priority.Value;
            }
        }

        await db.SaveChangesAsync();

        return Ok(new { count = eventUsers.Count });
    }
}
