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

    private async Task<bool> LoadAsync(int id)
    {
        EventData = await db.Events
            .Include(e => e.InviteCodes)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (EventData is null)
            return false;

        CurrentInviteCodes = EventData.InviteCodes
            .Where(ic => ic.ValidUntil > DateTime.UtcNow)
            .Select(ic =>
            {
                var validDays = (int)Math.Round((ic.ValidUntil - DateTime.UtcNow).TotalDays);
                var label = string.IsNullOrWhiteSpace(ic.Label) ? ic.Code : ic.Label;
                return new SelectListItem($"{label} (valid {validDays} days)", ic.Id.ToString(), ic.Id == NewInvite.InviteCodeId);
            })
            .ToList();

        var invitedUsers = db.EventUsers
            .Where(ue => ue.EventId == id)
            .Include(ue => ue.User)
            .Select(ue => new { ue.AssignedAccommodationCode, ue.User, InvitationEmailSent = ue.InviteEmailSent, ue.ScheduledFor })
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

    public record InviteeRow(string UserId, string DisplayName, string Email, string? AssignedAccommodationCode, string Status, bool InvitationEmailSent, DateTime? ScheduledFor);
}
