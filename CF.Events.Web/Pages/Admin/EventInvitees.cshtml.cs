using CF.Events.Web.Data;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Roles.Admin)]
public class EventInviteesModel(
    EventsDbContext db,
    IInvitationService inviteService,
    IToastNotification toastNotification) : PageModel
{

    public required Event EventData { get; set; }
    public List<SelectListItem> AccommodationCodes { get; private set; } = [];

    public UsersInviteRequest NewInvite { get; set; } = new();

    public List<InviteeRow> Invitees { get; private set; } = [];

    public List<SelectListItem> AvailableUsers { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        EventData = await db.Events.FirstAsync(e => e.Id == id);

        AccommodationCodes = EventData.AccommodationCodes
            .Select(ac => new SelectListItem(ac, ac))
            .ToList();

        var invitedUsers = db.EventUsers
            .Where(ue => ue.EventId == id)
            .Include(ue => ue.User)
            .Select(ue => new { ue.AssignedAccommodationCode, ue.User, InvitationEmailSent = ue.InviteEmailSent, SaveTheDateSent = ue.SaveTheDateEmailSent, ue.ScheduledFor })
            .ToList();
        var rsvps = db.Rsvps.Where(r => r.EventId == id).ToList();

        var unavailableUsers = new HashSet<string>();
        Invitees = invitedUsers
            .Select(iu =>
            {
                var user = iu.User;
                var rsvp = rsvps.FirstOrDefault(r => r.UserId == user.Id);
                var responded = rsvp?.SubmittedAt > DateTime.MinValue.AddDays(1);
                var status = responded ? (rsvp?.Attending == true ? AttendanceStatus.Attending : AttendanceStatus.Declined) : AttendanceStatus.Pending;
                unavailableUsers.Add(user.Id);
                return new InviteeRow(
                    user.Id,
                    user.DisplayName!,
                    user.Email!,
                    iu.AssignedAccommodationCode,
                    status,
                    iu.InvitationEmailSent,
                    iu.SaveTheDateSent,
                    iu.ScheduledFor);
            })
            .OrderBy(i => i.DisplayName)
            .ToList();

        AvailableUsers = await (from u in db.Users
                join ur in db.UserRoles on u.Id equals ur.UserId
                join r in db.Roles on ur.RoleId equals r.Id
                where u.IsActive && !unavailableUsers.Contains(u.Id) && r.Name == Roles.Guest
                orderby u.DisplayName
                select new SelectListItem($"{u.DisplayName} ({u.Email})", u.Id))
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int id, string? userId)
    {
        var userEvent = await db.EventUsers.FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);
        if (userEvent is null)
        {
            toastNotification.AddWarningToastMessage("Invitee not found");
            return RedirectToPage(new { id });
        }

        db.EventUsers.Remove(userEvent);
        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Invitee successfully removed");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostBulkRemoveAsync(int id, string userIds)
    {
        if (string.IsNullOrWhiteSpace(userIds))
        {
            toastNotification.AddWarningToastMessage("No users selected");
            return RedirectToPage(new { id });
        }

        var ids = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var userEvents = await db.EventUsers
            .Where(r => r.EventId == id && ids.Contains(r.UserId))
            .ToListAsync();

        if (userEvents.Count == 0)
        {
            toastNotification.AddWarningToastMessage("No invitees found to remove");
            return RedirectToPage(new { id });
        }

        db.EventUsers.RemoveRange(userEvents);
        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage($"Successfully removed {userEvents.Count} invitees");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSaveTheDateAsync(int id, string userId)
    {
        var @event = await db.Events.FindAsync(id);
        if (@event is null)
        {
            toastNotification.AddErrorToastMessage("Event not found");
            return RedirectToPage(new { id });
        }

        if (string.IsNullOrEmpty(@event.SaveDateTemplateId))
        {
            toastNotification.AddWarningToastMessage("Event is not eligible for Save the Date (no template ID set)");
            return RedirectToPage(new { id });
        }

        var user = await db.Users
            .Where(u => u.IsActive && u.Id == userId)
            .FirstOrDefaultAsync();
        if (user is null)
        {
            toastNotification.AddWarningToastMessage("User not found");
            return RedirectToPage(new { id });
        }

        await inviteService.SendEmail(new SaveDateEmailRequest
        {
            TemplateId = @event.SaveDateTemplateId,
            EventId = @event.Id,
            EventName = @event.Name,
            UserId = userId,
            UserName = user.DisplayName!,
            UserEmail = user.Email!
        });

        toastNotification.AddSuccessToastMessage($"Save the Date email sent to {user.DisplayName}");

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostBulkSaveTheDateAsync(int id, string userIds)
    {
        if (string.IsNullOrWhiteSpace(userIds))
        {
            toastNotification.AddWarningToastMessage("No users selected");
            return RedirectToPage(new { id });
        }

        var @event = await db.Events.FindAsync(id);
        if (@event is null)
        {
            toastNotification.AddErrorToastMessage("Event not found");
            return RedirectToPage(new { id });
        }

        if (string.IsNullOrEmpty(@event.SaveDateTemplateId))
        {
            toastNotification.AddWarningToastMessage("Event is not eligible for Save the Date (no template ID set)");
            return RedirectToPage(new { id });
        }

        var ids = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var requests = await db.EventUsers
            .Where(eu => eu.EventId == id && ids.Contains(eu.UserId) && eu.User.IsActive)
            .Select(eu => new SaveDateEmailRequest
            {
                TemplateId = eu.Event.SaveDateTemplateId!,
                EventId = eu.EventId,
                EventName = eu.Event.Name,
                UserName = eu.User.DisplayName!,
                UserId = eu.UserId,
                UserEmail = eu.User.Email!
            })
            .ToListAsync();

        if (requests.Count == 0)
        {
            toastNotification.AddWarningToastMessage("No users found or eligible for Save the Date");
            return RedirectToPage(new { id });
        }

        await inviteService.SendBatchedEmails(requests);

        toastNotification.AddSuccessToastMessage($"Successfully sent {requests.Count} Save the Date emails");

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSetInviteValidity(int id, int inviteValidity)
    {
        var count = await db.Events
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(e => e.InviteValidity, inviteValidity));

        if (count == 0)
            toastNotification.AddErrorToastMessage("Event not found");
        else toastNotification.AddSuccessToastMessage($"Invitation validity set to {inviteValidity} days");

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateAccommodationCodeAsync(int id, string userId, string? accommodationCode)
    {
        var userEvent = await db.EventUsers.FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);
        if (userEvent is null)
        {
            toastNotification.AddWarningToastMessage("Invitee not found");
            return RedirectToPage(new { id });
        }

        userEvent.AssignedAccommodationCode = string.IsNullOrWhiteSpace(accommodationCode) ? null : accommodationCode;
        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Accommodation code updated");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostBulkUpdateAccommodationCodesAsync(int id, Dictionary<string, string?> updates)
    {
        if (updates == null || updates.Count == 0)
        {
            toastNotification.AddWarningToastMessage("No updates to save");
            return RedirectToPage(new { id });
        }

        var userIds = updates.Keys.ToList();
        var userEvents = await db.EventUsers
            .Where(r => r.EventId == id && userIds.Contains(r.UserId))
            .ToListAsync();

        foreach (var userEvent in userEvents)
        {
            if (updates.TryGetValue(userEvent.UserId, out var code))
            {
                userEvent.AssignedAccommodationCode = string.IsNullOrWhiteSpace(code) ? null : code;
            }
        }

        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Accommodation codes updated successfully");
        return RedirectToPage(new { id });
    }

    public List<SelectListItem> GetAccommodationCodes(string? currentCode)
    {
        var list = EventData.AccommodationCodes
            .Select(ac => new SelectListItem(ac, ac, ac == currentCode))
            .ToList();
        list.Insert(0, new SelectListItem("None", "", string.IsNullOrEmpty(currentCode)));
        return list;
    }

    public record InviteeRow(string UserId, string DisplayName, string Email, string? AssignedAccommodationCode, AttendanceStatus Status, bool InvitationEmailSent, bool SaveTheDateSent, DateTime? ScheduledFor);
}

public enum AttendanceStatus
{
    Pending,
    Attending,
    Declined
}
