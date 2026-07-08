using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Constants.Roles.Admin)]
public class EventInviteesModel(
    EventsDbContext db,
    IToastNotification toastNotification) : PageModel
{
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
            .Select(ue => new { ue.AssignedAccommodationCode, ue.InviteCode, ue.User, InvitationEmailSent = ue.InviteEmailSent, ue.ScheduledFor })
            .ToList();
        var rsvps = db.Rsvps.Where(r => r.EventId == id).ToList();

        var unavailableUsers = new HashSet<string>();
        Invitees = invitedUsers
            .Select(iu =>
            {
                var user = iu.User;
                var rsvp = rsvps.FirstOrDefault(r => r.UserId == user.Id);
                var responded = rsvp?.SubmittedAt > DateTime.MinValue.AddDays(1);
                var status = responded ? (rsvp?.Attending == true ? "Attending" : "Declined") : "Pending";
                unavailableUsers.Add(user.Id);
                return new InviteeRow(
                    user.Id,
                    user.DisplayName!,
                    user.Email!,
                    iu.AssignedAccommodationCode,
                    GetInviteCodeLabel(iu.InviteCode),
                    status,
                    iu.InvitationEmailSent,
                    iu.ScheduledFor);
            })
            .OrderBy(i => i.DisplayName)
            .ToList();

        AvailableUsers = await db.Users
            .Where(u => u.IsActive && !unavailableUsers.Contains(u.Id))
            .OrderBy(u => u.DisplayName)
            .Select(u => new SelectListItem($"{u.DisplayName} ({u.Email})", u.Id))
            .ToListAsync();

        return true;
    }

    private static string GetInviteCodeLabel(InviteCode inviteCode)
    {
        var validDays = (int)Math.Round((inviteCode.ValidUntil - DateTime.UtcNow).TotalDays);
        var label = string.IsNullOrWhiteSpace(inviteCode.Label) ? inviteCode.Code : inviteCode.Label;
        return validDays <= 0 ? $"{label} (expired)" : $"{label} (valid {validDays} days)";
    }

    public record InviteeRow(string UserId, string DisplayName, string Email, string? AssignedAccommodationCode, string? InviteCode, string Status, bool InvitationEmailSent, DateTime? ScheduledFor);
}
