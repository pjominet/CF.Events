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
public class EventsModel(
    EventsDbContext db,
    UserManager<AppUser> userManager,
    IToastNotification toastNotification,
    IWebHostEnvironment env) : PageModel
{
    public List<Event> AllEvents { get; private set; } = [];

    public Dictionary<int, int> InviteeCounts { get; private set; } = [];

    public List<UserOption> AvailableUsers { get; private set; } = [];

    public bool ShowCreateModal { get; private set; }

    [BindProperty]
    public EventViewModel NewEvent { get; set; } = new() { Date = DateTime.Today.AddMonths(1) };

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            ShowCreateModal = true;
            return Page();
        }

        // Validate the upload (if any) before persisting the event so we can
        // store both the original display name and a URL-safe technical name.
        var (originalName, technicalName) = PrepareInvitationImage(NewEvent.InvitationImage);
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            ShowCreateModal = true;
            return Page();
        }

        var ev = new Event
        {
            Name = NewEvent.Name,
            Date = NewEvent.Date,
            Location = NewEvent.Location,
            Description = NewEvent.Description,
            InvitationFileName = technicalName,
            OriginalInvitationFileName = originalName,
            IsActive = true
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        // The on-disk folder is the event Id, which is only known after saving.
        if (technicalName is not null)
            await SaveInvitationImageAsync(ev.Id, NewEvent.InvitationImage!, technicalName);

        toastNotification.AddSuccessToastMessage("Event created successfully!");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage();
        }

        ev.IsActive = !ev.IsActive;
        await db.SaveChangesAsync();
        toastNotification.AddSuccessToastMessage($"Event {(ev.IsActive ? "activated" : "deactivated")} successfully");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostInviteAsync(int id, string? userId)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            toastNotification.AddWarningToastMessage("User not found");
            return RedirectToPage();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            toastNotification.AddWarningToastMessage("User not found");
            return RedirectToPage();
        }

        var alreadyInvited = await db.Rsvps.AnyAsync(r => r.EventId == id && r.UserId == user.Id);
        if (alreadyInvited)
        {
            toastNotification.AddWarningToastMessage("User already invited");
            return RedirectToPage();
        }

        db.Rsvps.Add(new Rsvp
        {
            EventId = id,
            UserId = user.Id,
            Attending = false,
            SubmittedAt = DateTime.MinValue
        });
        await db.SaveChangesAsync();

        if (!await userManager.IsInRoleAsync(user, Constants.Roles.User))
            await userManager.AddToRoleAsync(user, Constants.Roles.User);

        toastNotification.AddSuccessToastMessage($"{user.DisplayName ?? user.Email} invited successfully");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage();
        }

        var rsvps = await db.Rsvps.Where(r => r.EventId == id).ToListAsync();
        db.Rsvps.RemoveRange(rsvps);
        db.Events.Remove(ev);
        await db.SaveChangesAsync();

        DeleteInvitationImage(ev.Id);

        toastNotification.AddSuccessToastMessage("Event deleted successfully");
        return RedirectToPage();
    }

    public Dictionary<int, string> CurrentInviteCodes { get; private set; } = [];

    public async Task<IActionResult> OnPostRegenerateCodeAsync(int id)
    {
        var eventExists = await db.Events.AnyAsync(e => e.Id == id);
        if (!eventExists)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage();
        }

        var newCode = new InviteCode
        {
            EventId = id,
            Code = CodeGenerator.Generate(64),
            ValidUntil = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        db.InviteCodes.Add(newCode);
        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("New invite code generated");
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        AllEvents = await db.Events.Include(e => e.InviteCodes).OrderByDescending(e => e.Date).ToListAsync();

        var rsvps = await db.Rsvps.ToListAsync();
        InviteeCounts = rsvps
            .GroupBy(r => r.EventId)
            .ToDictionary(g => g.Key, g => g.Count());

        CurrentInviteCodes = AllEvents.ToDictionary(
            e => e.Id,
            e => e.InviteCodes
                .Where(c => c.ValidUntil > DateTime.UtcNow)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault()?.Code ?? "No valid code"
        );

        var allUsers = await userManager.Users
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserOption(u.Id, u.DisplayName ?? "undefined", u.Email ?? "undefined"))
            .ToListAsync();

        AvailableUsers = allUsers;
    }

    private void DeleteInvitationImage(int eventId)
    {
        try
        {
            var invitationsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Resources", "Invitations"));
            var dir = Path.GetFullPath(Path.Combine(invitationsRoot, eventId.ToString()));
            if (!dir.StartsWith(invitationsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                return;

            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup; ignore filesystem errors during deletion.
        }
    }

    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private (string? OriginalName, string? TechnicalName) PrepareInvitationImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return (null, null);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
        {
            ModelState.AddModelError("NewEvent.InvitationImage", "Unsupported image type. Use JPG, PNG, WEBP or GIF.");
            return (null, null);
        }

        var originalName = Path.GetFileName(file.FileName);
        var technicalName = Guid.NewGuid().ToString("N") + ext;
        return (originalName, technicalName);
    }

    private async Task SaveInvitationImageAsync(int eventId, IFormFile file, string technicalName)
    {
        var dir = Path.Combine(env.ContentRootPath, "Resources", "Invitations", eventId.ToString());
        Directory.CreateDirectory(dir);

        var fullPath = Path.Combine(dir, technicalName);
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);
    }

    public record UserOption(string Id, string DisplayName, string Email);
}
