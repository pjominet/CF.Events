using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Constants.Roles.Admin)]
public class EventInviteesModel(
    EventsDbContext db,
    UserManager<AppUser> userManager,
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
        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);
        if (rsvp is null)
        {
            toastNotification.AddWarningToastMessage("Invitee not found");
            return RedirectToPage(new { id });
        }

        db.Rsvps.Remove(rsvp);
        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Invitee successfully removed");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRegenerateCodeAsync(int id)
    {
        var ev = await db.Events.Include(e => e.InviteCodes).FirstOrDefaultAsync(e => e.Id == id);
        if (ev is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage(new { id });
        }

        var newCode = new InviteCode
        {
            EventId = id,
            Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant(),
            ValidUntil = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        db.InviteCodes.Add(newCode);
        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("New invite code generated");
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

        var rsvps = await db.Rsvps.Where(r => r.EventId == id).ToListAsync();
        var invitedUserIds = rsvps.Select(r => r.UserId).ToHashSet();

        var users = await userManager.Users.ToListAsync();
        var usersById = users.ToDictionary(u => u.Id);

        Invitees = rsvps
            .Select(r =>
            {
                usersById.TryGetValue(r.UserId, out var u);
                var responded = r.SubmittedAt > DateTime.MinValue.AddDays(1);
                var status = responded ? (r.Attending ? "Attending" : "Declined") : "Pending";
                return new InviteeRow(
                    r.UserId,
                    u?.DisplayName ?? "(unknown)",
                    u?.Email ?? "",
                    status);
            })
            .OrderBy(i => i.DisplayName)
            .ToList();

        AvailableUsers = users
            .Where(u => !invitedUserIds.Contains(u.Id))
            .OrderBy(u => u.DisplayName)
            .Select(u => new SelectListItem($"{u.DisplayName} ({u.Email})", u.Id))
            .ToList();

        return true;
    }

    public record InviteeRow(string UserId, string DisplayName, string Email, string Status);
}
