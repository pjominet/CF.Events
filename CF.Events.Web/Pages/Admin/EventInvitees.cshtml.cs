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
    public List<SelectListItem> CurrentInviteCodes { get; private set; } = [];
    public int InviteCodeId { get; set; }
    public bool SendEmailsOnInvite { get; set; } = true;
    public DateTime? ScheduledFor { get; set; }
    public bool AllowUseOfAccommodationCode { get; set; }

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
        // Find the invited person and remove them from their invitation
        var invitedPerson = await db.InvitedPersons
            .FirstOrDefaultAsync(ip => ip.Invitation.EventId == id && ip.UserId == userId);
        if (invitedPerson is null)
        {
            toastNotification.AddWarningToastMessage("Invitee not found");
            return RedirectToPage(new { id });
        }

        db.InvitedPersons.Remove(invitedPerson);
        await db.SaveChangesAsync();

        // If the invitation has no more people, remove it too
        var invitation = await db.Invitations
            .Include(i => i.InvitedPersons)
            .FirstOrDefaultAsync(i => i.Id == invitedPerson.InvitationId);
        if (invitation != null && invitation.InvitedPersons.Count == 0)
        {
            db.Invitations.Remove(invitation);
            await db.SaveChangesAsync();
        }

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
                return new SelectListItem($"{label} (valid {validDays} days)", ic.Id.ToString(), ic.Id == InviteCodeId);
            })
            .ToList();

        // Get all invited persons for this event with their invitations
        var invitedPersons = await db.InvitedPersons
            .Where(ip => ip.Invitation.EventId == id)
            .Include(ip => ip.Invitation)
            .Include(ip => ip.User)
            .Select(ip => new {
                ip.AssignedAccommodationCode,
                ip.User,
                InvitationEmailSent = ip.Invitation.InviteEmailSent,
                ScheduledFor = ip.Invitation.ScheduledFor,
                InvitationId = ip.InvitationId,
                ip.UserId
            })
            .ToListAsync();
        var rsvps = await db.Rsvps.Where(r => r.EventId == id).ToListAsync();

        var unavailableUsers = new HashSet<string>();
        Invitees = invitedPersons
            .Where(ip => ip.User != null)
            .Select(ip =>
            {
                var user = ip.User!;
                // Find RSVP for this invitation (group RSVP)
                var invitation = db.Invitations
                    .Include(i => i.Rsvp)
                    .FirstOrDefault(i => i.Id == ip.InvitationId);
                var rsvp = invitation?.Rsvp;
                var responded = rsvp?.SubmittedAt > DateTime.MinValue.AddDays(1);
                var status = responded ? (rsvp?.Status == RsvpStatus.Submitted ? "Attending" : "Declined") : "Pending";
                unavailableUsers.Add(user.Id);
                return new InviteeRow(
                    user.Id,
                    user.DisplayName ?? user.Email ?? "Unknown",
                    user.Email ?? "No email",
                    ip.AssignedAccommodationCode,
                    status,
                    ip.InvitationEmailSent,
                    ip.ScheduledFor);
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
