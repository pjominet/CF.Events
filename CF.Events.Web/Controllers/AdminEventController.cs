using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models.Requests;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreHero.ToastNotification.Abstractions;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[Route("admin/events/{eventId:int}")]
[Authorize(Roles = Roles.Admin)]
public class AdminEventController(
    IEventService eventService,
    IInvitationService invitationService,
    IExportService exportService,
    INotyfService toastNotification,
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
            toastNotification.Error(ex.Message);
            return RedirectToPage("/Admin/Events");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting invitees for event {EventId}", eventId);
            toastNotification.Error("An error occurred while exporting invitees.");
            return RedirectToPage("/Admin/Events");
        }
    }

    [HttpGet("rsvp-responses/{userId}")]
    public async Task<IActionResult> GetRsvpResponses([FromRoute] int eventId, [FromRoute] string userId)
    {
        var model = await eventService.GetRsvpResponsesAsync(eventId, userId);

        if (model is null) return NotFound();

        return PartialView("~/Pages/Admin/Shared/_RsvpResponsesModal.cshtml", model);
    }

    [HttpPost("invite-users")]
    public async Task<IActionResult> InviteUsers([FromRoute] int eventId, [FromForm] UsersInviteRequest inviteRequest)
    {
        if (inviteRequest.ScheduledFor.HasValue && inviteRequest.ScheduledFor.Value.ToUniversalTime() <= DateTime.UtcNow)
        {
            toastNotification.Warning("Scheduled time must be in the future");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        try
        {
            var count = await invitationService.InviteUsersAsync(eventId, inviteRequest);

            if (count == 0)
                toastNotification.Warning("All selected users are already invited to this event");
            else toastNotification.Success($"Successfully created {count} invitations");

            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }
        catch (ArgumentException ex)
        {
            toastNotification.Warning(ex.Message);
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inviting users for event {EventId}", eventId);
            toastNotification.Error("An error occurred while inviting users.");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }
    }

    [HttpPost("resend-invite")]
    public async Task<IActionResult> SendInvite([FromRoute] int eventId, [FromForm] string userId)
    {
        try
        {
            await invitationService.SendInvitesAsync(eventId, [userId]);
            toastNotification.Success("Successfully resent invitation");
        }
        catch (ArgumentException ex)
        {
            toastNotification.Warning(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resending invitation to {UserId} for event {EventId}", userId, eventId);
            toastNotification.Error("Invitation could not be resent");
        }

        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("resend-invites")]
    public async Task<IActionResult> SendInvites([FromRoute] int eventId, [FromForm] string userIds)
    {
        if (!userIds.HasValue())
        {
            toastNotification.Warning("No users selected");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var ids = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        try
        {
            await invitationService.SendInvitesAsync(eventId, ids);
            toastNotification.Success($"Successfully resent {ids.Count} invitations");
        }
        catch (ArgumentException ex)
        {
            toastNotification.Warning(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk resending invitations for event {EventId}", eventId);
            toastNotification.Error("Invitations could not be resent");
        }

        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("remove-invitee")]
    public async Task<IActionResult> RemoveInvitee([FromRoute] int eventId, [FromForm] string userId)
    {
        await eventService.RemoveInviteeAsync(eventId, userId);

        toastNotification.Success("Invitee successfully removed");
        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("remove-invitees")]
    public async Task<IActionResult> RemoveInvitees([FromRoute] int eventId, [FromForm] string userIds)
    {
        if (!userIds.HasValue())
        {
            toastNotification.Warning("No users selected");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var ids = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var count = await eventService.RemoveInviteesAsync(eventId, ids);

        if (count == 0)
        {
            toastNotification.Warning("No invitees found to remove");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        toastNotification.Success($"Successfully removed {count} invitees");
        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("send-save-the-date")]
    public async Task<IActionResult> SendSaveTheDate([FromRoute] int eventId, [FromForm] string userId)
    {
        var result = await invitationService.SendSaveTheDateAsync(eventId, userId);

        switch (result.Status)
        {
            case EmailSendResultStatus.Success:
                toastNotification.Success($"Save the Date email sent to {result.Message}");
                break;
            case EmailSendResultStatus.EventNotFound:
                toastNotification.Error(result.Message!);
                break;
            case EmailSendResultStatus.TemplateMissing:
            case EmailSendResultStatus.UserNotFound:
            case EmailSendResultStatus.Failed:
            default:
                toastNotification.Warning(result.Message!);
                break;
        }

        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("send-save-the-dates")]
    public async Task<IActionResult> SendSaveTheDates([FromRoute] int eventId, [FromForm] string userIds)
    {
        if (!userIds.HasValue())
        {
            toastNotification.Warning("No users selected");
            return LocalRedirect($"/admin/events/{eventId}/invitees");
        }

        var ids = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var result = await invitationService.SendBulkSaveTheDateAsync(eventId, ids);

        switch (result.Status)
        {
            case EmailSendResultStatus.Success:
                toastNotification.Success($"Successfully sent {result.SentCount} Save the Date emails");
                break;
            case EmailSendResultStatus.TemplateMissing:
            case EmailSendResultStatus.UserNotFound:
            case EmailSendResultStatus.EventNotFound:
            case EmailSendResultStatus.Failed:
            default:
                toastNotification.Error(result.Message!);
                break;
        }

        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpGet("admin-rsvp/{userId}")]
    public async Task<IActionResult> GetAdminRsvpForm([FromRoute] int eventId, [FromRoute] string userId)
    {
        var (model, maxParticipants, eventDuration) = await eventService.GetAdminRsvpDataAsync(eventId, userId);

        return PartialView("~/Pages/Admin/Shared/_AdminRsvpForm.cshtml", (eventId, maxParticipants, eventDuration, userId, model));
    }

    [HttpPost("admin-rsvp/{userId}")]
    public async Task<IActionResult> AdminRsvp([FromRoute] int eventId, [FromRoute] string userId, [FromForm] RsvpRequest newRsvp)
    {
        try
        {
            await eventService.UpdateAdminRsvpAsync(eventId, userId, newRsvp);
            toastNotification.Success($"RSVP updated for guest {userId}");
        }
        catch (ArgumentException ex)
        {
            toastNotification.Error(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating RSVP for user {UserId} and event {EventId}", userId, eventId);
            toastNotification.Error("An error occurred while updating RSVP.");
        }

        return LocalRedirect($"/admin/events/{eventId}/invitees");
    }

    [HttpPost("update-invitees")]
    public async Task<IActionResult> UpdateInvitees([FromRoute] int eventId, [FromBody] List<InviteeUpdateRequest> updates)
    {
        var count = await eventService.UpdateInviteesAsync(eventId, updates);

        if (count > 0)
        {
            var message = $"Successfully updated {count} invitee{(count > 1 ? "s" : "")}";
            toastNotification.Success(message);
        }

        return Ok(new { count });
    }

    [HttpPost("notes")]
    public async Task<IActionResult> UpdateInviteeNotes([FromRoute] int eventId, [FromBody] InviteeNotesRequest request)
    {
        var isSuccess = await eventService.UpdateInviteeNotesAsync(eventId, request.UserId, request.Notes);

        if (isSuccess)
        {
            toastNotification.Success("Notes updated");
            return NoContent();
        }

        toastNotification.Error("Failed to update notes");
        return BadRequest();
    }
}
