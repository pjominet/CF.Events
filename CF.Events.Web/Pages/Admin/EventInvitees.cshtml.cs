using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Models;
using CF.Events.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Constants.Roles.Admin)]
public class EventInviteesModel(
    EventsDbContext db,
    UserManager<ApplicationUser> userManager,
    IToastNotification toastNotification) : PageModel
{
    public Event? EventData { get; private set; }

    public List<InviteeRow> Invitees { get; private set; } = [];

    public List<UserOption> AvailableUsers { get; private set; } = [];

    public bool ShowInviteModal { get; private set; }

    [BindProperty]
    public InviteUserInput Invite { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id))
            return NotFound();

        Invite.Password = TempPasswordGenerator.Generate();
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync(int id, string? userId)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage(new { id });
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            toastNotification.AddWarningToastMessage("User not found");
            return RedirectToPage(new { id });
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            toastNotification.AddWarningToastMessage("User not found");
            return RedirectToPage(new { id });
        }

        await InviteUserToEventAsync(id, user);
        toastNotification.AddSuccessToastMessage($"{user.DisplayName ?? user.Email} successfully invited");
        return RedirectToPage(new { id });
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

    public async Task<IActionResult> OnPostInviteAsync(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage(new { id });
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(id);
            ShowInviteModal = true;
            return Page();
        }

        var existing = await userManager.FindByEmailAsync(Invite.Email);
        if (existing is not null)
        {
            await InviteUserToEventAsync(id, existing);
            toastNotification.AddSuccessToastMessage($"{existing.DisplayName ?? existing.Email} was already registered and has been invited");
            return RedirectToPage(new { id });
        }

        var user = new ApplicationUser
        {
            UserName = Invite.Email,
            Email = Invite.Email,
            DisplayName = Invite.DisplayName,
            MustChangePassword = true
        };

        var result = await userManager.CreateAsync(user, Invite.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            await LoadAsync(id);
            ShowInviteModal = true;
            return Page();
        }

        await InviteUserToEventAsync(id, user);
        toastNotification.AddSuccessToastMessage($"Invitation created for {Invite.Email}. Temporary password: {Invite.Password}");
        return RedirectToPage(new { id });
    }

    private async Task InviteUserToEventAsync(int eventId, ApplicationUser user)
    {
        var alreadyInvited = await db.Rsvps.AnyAsync(r => r.EventId == eventId && r.UserId == user.Id);
        if (!alreadyInvited)
        {
            db.Rsvps.Add(new Rsvp
            {
                EventId = eventId,
                UserId = user.Id,
                Attending = false,
                SubmittedAt = DateTime.MinValue
            });
            await db.SaveChangesAsync();
        }

        if (!await userManager.IsInRoleAsync(user, Constants.Roles.User))
            await userManager.AddToRoleAsync(user, Constants.Roles.User);
    }

    private async Task<bool> LoadAsync(int id)
    {
        EventData = await db.Events.FindAsync(id);
        if (EventData is null)
            return false;

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
            .Select(u => new UserOption(u.Id, u.DisplayName ?? "", u.Email ?? ""))
            .ToList();

        return true;
    }

    public record InviteeRow(string UserId, string DisplayName, string Email, string Status);

    public record UserOption(string Id, string DisplayName, string Email);
}
