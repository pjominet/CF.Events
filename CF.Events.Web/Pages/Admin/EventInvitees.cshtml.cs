using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Models;
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
    public string CurrentInviteCode { get; private set; } = "No valid code";

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
        var userEvent = await db.UserEvents.FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);
        if (userEvent is null)
        {
            toastNotification.AddWarningToastMessage("Invitee not found");
            return RedirectToPage(new { id });
        }

        db.UserEvents.Remove(userEvent);
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

        CurrentInviteCode = EventData.InviteCodes
            .Where(c => c.ValidUntil > DateTime.UtcNow)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault()?.Code ?? "No valid code";

        var invitedUsers = db.UserEvents.Where(ue => ue.EventId == id).Select(ue => ue.User!).ToList();
        var rsvps = db.Rsvps.Where(r => r.EventId == id).ToList();

        Invitees = invitedUsers
            .Select(u =>
            {
                var rsvp = rsvps.FirstOrDefault(r => r.UserId == u.Id);
                var responded = rsvp?.SubmittedAt > DateTime.MinValue.AddDays(1);
                var status = responded ? (rsvp?.Attending == true ? "Attending" : "Declined") : "Pending";
                return new InviteeRow(
                    u.Id,
                    u.DisplayName!,
                    u.Email!,
                    status);
            })
            .OrderBy(i => i.DisplayName)
            .ToList();

        var unavailableUsers = invitedUsers.Select(i => i.Id);
        AvailableUsers = await db.Users
            .Where(u => u.IsActive && !unavailableUsers.Contains(u.Id))
            .OrderBy(u => u.DisplayName)
            .Select(u => new SelectListItem($"{u.DisplayName} ({u.Email})", u.Id))
            .ToListAsync();

        return true;
    }

    public record InviteeRow(string UserId, string DisplayName, string Email, string Status);
}
