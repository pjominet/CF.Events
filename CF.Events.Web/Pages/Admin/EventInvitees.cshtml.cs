using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Roles.Admin)]
public class EventInviteesModel(
    EventsDbContext db,
    IOptions<AppSettings> appOptions,
    IMailService mailService,
    IToastNotification toastNotification) : PageModel
{
    private readonly AppSettings _appSettings = appOptions.Value;

    public Event? EventData { get; private set; }
    public List<SelectListItem> CurrentInviteCodes { get; private set; } = [];
    public List<SelectListItem> AccommodationCodes { get; private set; } = [];

    public UsersInviteRequest NewInvite { get; set; } = new();

    public List<InviteeRow> Invitees { get; private set; } = [];

    public List<SelectListItem> AvailableUsers { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id))
            return NotFound();

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

        await mailService.SendSaveTheDateAsync(@event.SaveDateTemplateId, @event.Name, user.DisplayName!, user.Email!, _appSettings.BaseUrl ?? string.Empty);

        var eventUser = await db.EventUsers.FirstOrDefaultAsync(eu => eu.EventId == id && eu.UserId == userId);
        if (eventUser != null)
        {
            eventUser.SaveTheDateEmailSent = true;
            await db.SaveChangesAsync();
        }

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
        var users = await db.Users
            .Where(u => ids.Contains(u.Id) && u.IsActive)
            .ToListAsync();

        if (users.Count == 0)
        {
            toastNotification.AddWarningToastMessage("No users found");
            return RedirectToPage(new { id });
        }

        var count = 0;
        foreach (var user in users)
        {
            await mailService.SendSaveTheDateAsync(@event.SaveDateTemplateId, @event.Name, user.DisplayName!, user.Email!, _appSettings.BaseUrl ?? string.Empty);
            count++;
        }

        var eventUsers = await db.EventUsers
            .Where(eu => eu.EventId == id && ids.Contains(eu.UserId))
            .ToListAsync();

        foreach (var eu in eventUsers)
        {
            eu.SaveTheDateEmailSent = true;
        }
        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage($"Successfully sent {count} Save the Date emails");

        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id)
    {
        EventData = await db.Events
            .Include(e => e.InviteCodes)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (EventData is null)
            return false;

        CurrentInviteCodes = EventData.InviteCodes
            .Where(ic => ic.ValidUntil > DateTime.UtcNow)
            .Select(ic => new SelectListItem(GetInviteCodeLabel(ic), ic.Id.ToString(), ic.Id == NewInvite.InviteCodeId))
            .ToList();

        AccommodationCodes = EventData.AccommodationCodes
            .Select(ac => new SelectListItem(ac, ac))
            .ToList();

        var invitedUsers = db.EventUsers
            .Where(ue => ue.EventId == id)
            .Include(ue => ue.User)
            .Select(ue => new { ue.AssignedAccommodationCode, ue.InviteCode, ue.User, InvitationEmailSent = ue.InviteEmailSent, SaveTheDateSent = ue.SaveTheDateEmailSent, ue.ScheduledFor })
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
                    GetInviteCodeLabel(iu.InviteCode),
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

        return true;
    }

    private static string GetInviteCodeLabel(InviteCode inviteCode)
    {
        var validDays = (int)Math.Round((inviteCode.ValidUntil - DateTime.UtcNow).TotalDays);
        var label = string.IsNullOrWhiteSpace(inviteCode.Label) ? inviteCode.Code : inviteCode.Label;
        return validDays <= 0 ? $"{label} (expired)" : $"{label} (valid {validDays} days)";
    }

    public record InviteeRow(string UserId, string DisplayName, string Email, string? AssignedAccommodationCode, string? InviteCode, AttendanceStatus Status, bool InvitationEmailSent, bool SaveTheDateSent, DateTime? ScheduledFor);
}

public enum AttendanceStatus
{
    Pending,
    Attending,
    Declined
}
