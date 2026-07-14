using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Infrastructure.ModelBinders;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Roles.Admin)]
public class EditEventModel(
    EventsDbContext db,
    IFileService fileService,
    IToastNotification toastNotification,
    IWebHostEnvironment env) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id.HasValue)
        {
            var @event = await db.Events
                .Include(e => e.BookingLinks)
                .Include(e => e.EventFaq)
                .Include(e => e.EventSchedule)
                .FirstOrDefaultAsync(e => e.Id == id.Value);

            if (@event is null)
            {
                toastNotification.AddErrorToastMessage("Event not found");
                return RedirectToPage("/Admin/Events");
            }

            Input = new InputModel
            {
                Id = @event.Id,
                Name = @event.Name,
                StartDate = @event.StartDate,
                EndDate = @event.EndDate,
                Location = @event.Location,
                Description = @event.Description,
                TravelInstructions = @event.TravelInstructions,
                AccommodationCodes = @event.AccommodationCodes,
                AccommodationDetails = @event.AccommodationDetails,
                SaveDateEmailTemplateId = @event.SaveDateTemplateId,
                InvitationEmailTemplateId = @event.InvitationTemplateId,
                DonationType = GetDonationType(@event),
                DonationIban = @event.DonationIban,
                DonationLink = @event.DonationLink,
                BookingLinks = @event.BookingLinks.Select(bl => bl.Link).ToList(),
                FaqItems = @event.EventFaq.Select(f => new FaqInputModel { Question = f.Question, Answer = f.Answer }).ToList(),
                ScheduleSteps = @event.EventSchedule.Select(s => new ScheduleInputModel { Day = s.Day, TimeStamp = s.TimeStamp, Label = s.Label }).ToList()
            };
        }
        else
        {
            Input = new InputModel
            {
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(1),
                DonationType = DonationType.None
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // Handle Image
        var (originalName, technicalName) = PrepareInvitationImage(Input.InvitationImage);
        if (!ModelState.IsValid) return Page();

        Event? @event;
        var isNew = Input.Id == 0;

        if (isNew)
        {
            @event = new Event { CreatedAt = DateTime.UtcNow };
            db.Events.Add(@event);
        }
        else
        {
            @event = await db.Events
                .Include(e => e.BookingLinks)
                .Include(e => e.EventFaq)
                .Include(e => e.EventSchedule)
                .FirstOrDefaultAsync(e => e.Id == Input.Id);

            if (@event is null)
            {
                toastNotification.AddErrorToastMessage("Event not found");
                return RedirectToPage("/Admin/Events");
            }
        }

        @event.Name = Input.Name;
        @event.StartDate = Input.StartDate;
        @event.EndDate = Input.EndDate;
        @event.Location = Input.Location;
        @event.Description = Input.Description;
        @event.TravelInstructions = Input.TravelInstructions;
        @event.AccommodationCodes = Input.AccommodationCodes;
        @event.AccommodationDetails = Input.AccommodationDetails;
        @event.SaveDateTemplateId = Input.SaveDateEmailTemplateId;
        @event.InvitationTemplateId = Input.InvitationEmailTemplateId;

        @event.DonationIban = Input.DonationType is DonationType.Iban ? Input.DonationIban : null;
        @event.DonationLink = Input.DonationType is DonationType.Link ? Input.DonationLink : null;

        // Booking Links
        @event.BookingLinks.Clear();
        foreach (var link in Input.BookingLinks.Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var trimmedLink = link.Trim();
            var type = LinkType.Web;
            if (trimmedLink.IsEmail()) type = LinkType.Email;
            else if (trimmedLink.IsPhoneNumber()) type = LinkType.Phone;
            else if (!trimmedLink.StartsWith("http", StringComparison.OrdinalIgnoreCase)) trimmedLink = $"https://{trimmedLink}";

            @event.BookingLinks.Add(new BookingLink { Link = trimmedLink, Type = type });
        }

        // FAQ
        @event.EventFaq.Clear();
        foreach (var faq in Input.FaqItems.Where(f => !string.IsNullOrWhiteSpace(f.Question) && !string.IsNullOrWhiteSpace(f.Answer)))
        {
            @event.EventFaq.Add(new EventFaqItem { Question = faq.Question, Answer = faq.Answer });
        }

        // Schedule
        @event.EventSchedule.Clear();
        foreach (var step in Input.ScheduleSteps.Where(s => !string.IsNullOrWhiteSpace(s.Label)))
        {
            @event.EventSchedule.Add(new EventScheduleStep { Day = step.Day, TimeStamp = step.TimeStamp, Label = step.Label });
        }

        if (technicalName is not null)
        {
            if (!isNew && !string.IsNullOrEmpty(@event.InvitationFileName))
                fileService.DeleteInvitationImage(@event.Id, @event.InvitationFileName);

            @event.InvitationFileName = technicalName;
            @event.OriginalInvitationFileName = originalName;
        }

        await db.SaveChangesAsync();

        if (technicalName is not null)
            await SaveInvitationImageAsync(@event.Id, Input.InvitationImage!, technicalName);

        toastNotification.AddSuccessToastMessage($"Event {(isNew ? "created" : "updated")} successfully!");
        return RedirectToPage("/Admin/Events");
    }

    private (string? OriginalName, string? TechnicalName) PrepareInvitationImage(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return (null, null);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        string[] allowed = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
        if (allowed.Contains(ext))
            return (Path.GetFileName(file.FileName), Guid.NewGuid().ToString("N") + ext);

        ModelState.AddModelError("Input.InvitationImage", "Unsupported image type.");
        return (null, null);
    }

    private async Task SaveInvitationImageAsync(int eventId, IFormFile file, string technicalName)
    {
        var dir = Path.Combine(env.ContentRootPath, "Resources", "Invitations", eventId.ToString());
        Directory.CreateDirectory(dir);
        await using var stream = System.IO.File.Create(Path.Combine(dir, technicalName));
        await file.CopyToAsync(stream);
    }

    private static DonationType GetDonationType(Event @event)
    {
        return !string.IsNullOrEmpty(@event.DonationIban)
            ? DonationType.Iban : !string.IsNullOrEmpty(@event.DonationLink)
                ? DonationType.Link : DonationType.None;
    }

    public class InputModel
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public string? TravelInstructions { get; set; }
        [ModelBinder(BinderType = typeof(FlatListModelBinder))]
        public List<string> AccommodationCodes { get; set; } = [];
        public string? AccommodationDetails { get; set; }
        public string? SaveDateEmailTemplateId { get; set; }
        public string? InvitationEmailTemplateId { get; set; }
        public DonationType DonationType { get; set; }
        public string? DonationIban { get; set; }
        public string? DonationLink { get; set; }
        [ModelBinder(BinderType = typeof(FlatListModelBinder))]
        public List<string> BookingLinks { get; set; } = [];
        public IFormFile? InvitationImage { get; set; }
        public List<FaqInputModel> FaqItems { get; set; } = [];
        public List<ScheduleInputModel> ScheduleSteps { get; set; } = [];
    }

    public class FaqInputModel
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class ScheduleInputModel
    {
        public int Day { get; set; }
        public TimeOnly TimeStamp { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public enum DonationType
    {
        None,
        Iban,
        Link
    }
}
