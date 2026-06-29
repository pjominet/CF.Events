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

        Event? @event;
        var createMode = NewEvent.Id == 0;
        if (createMode)
        {
            @event = new Event { CreatedAt = DateTime.UtcNow };
            db.Events.Add(@event);
        }
        else
        {
            @event = await db.Events.Include(e => e.EventConfig).FirstOrDefaultAsync(e => e.Id == NewEvent.Id);
            if (@event is null)
            {
                toastNotification.AddErrorToastMessage("Event not found");
                return RedirectToPage();
            }
        }

        @event.Name = NewEvent.Name;
        @event.Date = NewEvent.Date;
        @event.Location = NewEvent.Location;
        @event.Description = NewEvent.Description;
        @event.AccommodationCode = NewEvent.AccommodationCode;

        if (technicalName is not null)
        {
            if (!createMode && !string.IsNullOrEmpty(@event.InvitationFileName))
                DeleteInvitationImage(@event.Id, @event.InvitationFileName);
            @event.InvitationFileName = technicalName;
            @event.OriginalInvitationFileName = originalName;
        }

        @event.EventConfig ??= new EventConfig { EventId = @event.Id };
        @event.EventConfig.OfferDinner = NewEvent.OfferDinner;
        @event.EventConfig.OfferLunch = NewEvent.OfferLunch;
        @event.EventConfig.OfferBreakfast = NewEvent.OfferBreakfast;
        @event.EventConfig.OfferBrunch = NewEvent.OfferBrunch;
        @event.EventConfig.ShowAccommodationOptions = NewEvent.ShowAccommodationOptions;
        @event.EventConfig.AllowComments = NewEvent.AllowComments;
        @event.EventConfig.AllowPartners = NewEvent.AllowPartners;
        @event.EventConfig.AllowKids = NewEvent.AllowKids;

        await db.SaveChangesAsync();

        if (technicalName is not null)
            await SaveInvitationImageAsync(@event.Id, NewEvent.InvitationImage!, technicalName);

        toastNotification.AddSuccessToastMessage($"Event {(createMode ? "created" : "updated")} successfully!");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var @event = await db.Events.FindAsync(id);
        if (@event is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage();
        }

        @event.IsActive = !@event.IsActive;
        await db.SaveChangesAsync();
        toastNotification.AddSuccessToastMessage($"Event {(@event.IsActive ? "activated" : "deactivated")} successfully");
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

    public string GetSerializedEvent(Event @event)
    {
        return JsonSerializer.Serialize(new
        {
            id = @event.Id,
            name = @event.Name,
            date = @event.Date.ToString("yyyy-MM-dd"),
            location = @event.Location,
            description = @event.Description,
            offerDinner = @event.EventConfig?.OfferDinner ?? false,
            offerLunch = @event.EventConfig?.OfferLunch ?? false,
            offerBreakfast = @event.EventConfig?.OfferBreakfast ?? false,
            offerBrunch = @event.EventConfig?.OfferBrunch ?? false,
            accommodationCode = @event.AccommodationCode,
            showAccommodationOptions = @event.EventConfig?.ShowAccommodationOptions ?? false,
            allowComments = @event.EventConfig?.AllowComments ?? true,
            allowPartners = @event.EventConfig?.AllowPartners ?? true,
            allowKids = @event.EventConfig?.AllowKids ?? true,
            originalInvitationFileName = @event.OriginalInvitationFileName
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

    private void DeleteInvitationImage(int eventId, string? fileName = null)
    {
        try
        {
            var invitationsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Resources", "Invitations"));
            var dir = Path.GetFullPath(Path.Combine(invitationsRoot, eventId.ToString()));
            if (!dir.StartsWith(invitationsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                return;

            if (string.IsNullOrEmpty(fileName))
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            else
            {
                var filePath = Path.Combine(dir, fileName);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
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

        [StringLength(100)]
        public string? AccommodationCode { get; init; }

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
