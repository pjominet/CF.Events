using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Infrastructure.ModelBinders;
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

    private static DateTime _initEventDate = DateTime.Today.AddDays(1);

    [BindProperty]
    public InputModel NewEvent { get; set; } = new()
    {
        StartDate = _initEventDate,
        EndDate = _initEventDate
    };

    private JsonSerializerOptions jsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
            @event = await db.Events.FirstOrDefaultAsync(e => e.Id == NewEvent.Id);
            if (@event is null)
            {
                toastNotification.AddErrorToastMessage("Event not found");
                return RedirectToPage();
            }
        }

        @event.Name = NewEvent.Name;
        @event.StartDate = NewEvent.StartDate;
        @event.EndDate = NewEvent.EndDate;
        @event.Location = NewEvent.Location;
        @event.Description = NewEvent.Description;
        @event.AccommodationCodes = NewEvent.AccommodationCodes;
        @event.AccommodationDetails = NewEvent.AccommodationDetails;
        @event.SaveDateTemplateId = NewEvent.SaveDateEmailTemplateId;
        @event.InvitationTemplateId = NewEvent.InvitationEmailTemplateId;
        @event.DonationIban = NewEvent.DonationIban;

        // fix duplicate save on update
        @event.BookingLinks = NewEvent.BookingLinks.Select(link =>
        {
            link = link.Trim();
            if (link.IsEmail())
                return new BookingLink{ Link = link, Type = LinkType.Email};

            if (link.IsPhoneNumber())
                return new BookingLink{ Link = link, Type = LinkType.Phone};

            if (!(link.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                link.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                return new BookingLink{ Link = $"https://{link}", Type = LinkType.Web};

            return new BookingLink{ Link = link, Type = LinkType.Web};
        }).ToList();

        if (technicalName is not null)
        {
            if (!createMode && !string.IsNullOrEmpty(@event.InvitationFileName))
                DeleteInvitationImage(@event.Id, @event.InvitationFileName);
            @event.InvitationFileName = technicalName;
            @event.OriginalInvitationFileName = originalName;
        }

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
        var @event = await db.Events.FindAsync(id);
        if (@event is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage();
        }

        var rsvps = await db.Rsvps.Where(r => r.EventId == id).ToListAsync();
        db.Rsvps.RemoveRange(rsvps);
        db.Events.Remove(@event);
        await db.SaveChangesAsync();

        DeleteInvitationImage(@event.Id);

        toastNotification.AddSuccessToastMessage("Event deleted successfully");
        return RedirectToPage();
    }

    public string GetEventAsJson(Event @event)
    { return JsonSerializer.Serialize(new
        {
            id = @event.Id,
            name = @event.Name,
            startDate = @event.StartDate.ToString("yyyy-MM-dd"),
            endDate = @event.EndDate.ToString("yyyy-MM-dd"),
            location = @event.Location,
            description = @event.Description,
            accommodationCodes = @event.AccommodationCodes,
            accommodationDetails = @event.AccommodationDetails,
            saveDateTemplateId = @event.SaveDateTemplateId,
            invitationTemplateId = @event.InvitationTemplateId,
            donationIban = @event.DonationIban,
            bookingLinks = @event.BookingLinks.Select(bl => bl.Link),
            originalInvitationFileName = @event.OriginalInvitationFileName
        }, jsonOptions);
    }

    private async Task LoadAsync()
    {
        AllEvents = await db.Events
            .Include(e => e.BookingLinks)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync();

        var eventUsers = await db.EventUsers.ToListAsync();
        InviteeCounts = eventUsers
            .GroupBy(r => r.EventId)
            .ToDictionary(g => g.Key, g => g.Count());
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

        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }

        public string? Location { get; init; }

        [StringLength(500)]
        public string? Description { get; init; }

        [ModelBinder(BinderType = typeof(FlatListModelBinder))]
        public List<string> AccommodationCodes { get; init; } = [];

        [StringLength(1000)]
        public string? AccommodationDetails { get; init; }

        [StringLength(255)]
        public string? SaveDateEmailTemplateId { get; init; }

        [StringLength(255)]
        public string? InvitationEmailTemplateId { get; init; }

        [StringLength(64)]
        public string? DonationIban { get; init; }

        [ModelBinder(BinderType = typeof(FlatListModelBinder))]
        public List<string> BookingLinks { get; init; } = [];

        public IFormFile? InvitationImage { get; init; }
    }
}
