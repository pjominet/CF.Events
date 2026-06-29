using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CF.Events.Web.Data;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Roles.Admin)]
public class EventsModel(
    EventsDbContext db,
    IToastNotification toastNotification,
    IWebHostEnvironment env) : PageModel
{
    public List<Event> AllEvents { get; private set; } = [];

    public Dictionary<int, int> InviteeCounts { get; private set; } = [];

    [BindProperty]
    public InputModel NewEvent { get; set; } = new() { Date = DateTime.Today.AddMonths(1) };

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            ViewData[ViewDataKeys.ShowEventModal] = true;
            return Page();
        }

        var (originalName, technicalName) = PrepareInvitationImage(NewEvent.InvitationImage);
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            ViewData[ViewDataKeys.ShowEventModal] = true;
            return Page();
        }

        Event ev;
        bool isNew = NewEvent.Id == 0;

        if (isNew)
        {
            ev = new Event
            {
                CreatedAt = DateTime.UtcNow
            };
            db.Events.Add(ev);
        }
        else
        {
            ev = await db.Events.Include(e => e.EventConfig).FirstOrDefaultAsync(e => e.Id == NewEvent.Id);
            if (ev is null)
            {
                toastNotification.AddErrorToastMessage("Event not found");
                return RedirectToPage();
            }
        }

        ev.Name = NewEvent.Name;
        ev.Date = NewEvent.Date;
        ev.Location = NewEvent.Location;
        ev.Description = NewEvent.Description;

        if (technicalName is not null)
        {
            if (!isNew && !string.IsNullOrEmpty(ev.InvitationFileName))
            {
                // Optional: Delete old image if replacing
            }
            ev.InvitationFileName = technicalName;
            ev.OriginalInvitationFileName = originalName;
        }

        ev.EventConfig ??= new EventConfig { EventId = ev.Id };
        ev.EventConfig.OfferDinner = NewEvent.OfferDinner;
        ev.EventConfig.OfferLunch = NewEvent.OfferLunch;
        ev.EventConfig.OfferBreakfast = NewEvent.OfferBreakfast;
        ev.EventConfig.OfferBrunch = NewEvent.OfferBrunch;
        ev.EventConfig.ShowAccommodationOptions = NewEvent.ShowAccommodationOptions;
        ev.EventConfig.AllowComments = NewEvent.AllowComments;
        ev.EventConfig.AllowPartners = NewEvent.AllowPartners;
        ev.EventConfig.AllowKids = NewEvent.AllowKids;

        await db.SaveChangesAsync();

        if (technicalName is not null)
            await SaveInvitationImageAsync(ev.Id, NewEvent.InvitationImage!, technicalName);

        toastNotification.AddSuccessToastMessage($"Event {(isNew ? "created" : "updated")} successfully!");
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

    public string GetSerializedEvent(Event eventData)
    {
        return JsonSerializer.Serialize(new
        {
            id = eventData.Id,
            name = eventData.Name,
            date = eventData.Date.ToString("yyyy-MM-dd"),
            location = eventData.Location,
            description = eventData.Description,
            offerDinner = eventData.EventConfig?.OfferDinner ?? false,
            offerLunch = eventData.EventConfig?.OfferLunch ?? false,
            offerBreakfast = eventData.EventConfig?.OfferBreakfast ?? false,
            offerBrunch = eventData.EventConfig?.OfferBrunch ?? false,
            showAccommodationOptions = eventData.EventConfig?.ShowAccommodationOptions ?? false,
            allowComments = eventData.EventConfig?.AllowComments ?? true,
            allowPartners = eventData.EventConfig?.AllowPartners ?? true,
            allowKids = eventData.EventConfig?.AllowKids ?? true
        });
    }

    public Dictionary<int, string> CurrentInviteCodes { get; private set; } = [];

    private async Task LoadAsync()
    {
        AllEvents = await db.Events
            .Include(e => e.InviteCodes)
            .Include(e => e.EventConfig)
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        var eventUsers = await db.UserEvents.ToListAsync();
        InviteeCounts = eventUsers
            .GroupBy(r => r.EventId)
            .ToDictionary(g => g.Key, g => g.Count());

        CurrentInviteCodes = AllEvents.ToDictionary(
            e => e.Id,
            e => e.InviteCodes
                .Where(c => c.ValidUntil > DateTime.UtcNow)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault()?.Code ?? "No valid code"
        );
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

    public sealed class InputModel
    {
        public int Id { get; init; }

        [Required]
        [StringLength(100)]
        public string Name { get; init; } = string.Empty;

        public DateTime Date { get; init; }

        public string? Location { get; init; }

        [StringLength(500)]
        public string? Description { get; init; }

        public IFormFile? InvitationImage { get; init; }

        public bool OfferDinner { get; init; }
        public bool OfferLunch { get; init; }
        public bool OfferBreakfast { get; init; }
        public bool OfferBrunch { get; init; }
        public bool ShowAccommodationOptions { get; init; }
        public bool AllowComments { get; init; } = true;
        public bool AllowPartners { get; init; } = true;
        public bool AllowKids { get; init; } = true;
    }
}
