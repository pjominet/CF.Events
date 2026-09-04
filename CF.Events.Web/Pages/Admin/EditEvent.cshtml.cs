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
    IToastNotification toastNotification) : PageModel
{
    [BindProperty] public EventModel Event { get; set; } = new();

    [BindProperty] public string? RedirectAfterSave { get; set; }

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

            Event = new EventModel
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
                MaxParticipantsPerRsvp = @event.MaxParticipantsPerRsvp,
                SendWithLink = @event.EmailWithLink,
                DonationTypes = GetDonationTypes(@event),
                DonationIban = @event.DonationIban,
                DonationLink = @event.DonationLink,
                BookingLinks = [.. @event.BookingLinks.Select(bl => bl.Link)],
                IsFinalised = @event.IsFinalised,
                FaqItems =
                [
                    .. @event.EventFaq.OrderBy(f => f.SortOrder)
                        .Select(f => new FaqInputModel
                    {
                        Question = f.Question,
                        Answer = f.Answer,
                        SortOrder = f.SortOrder
                    })
                ],
                ScheduleSteps =
                [
                    .. @event.EventSchedule.OrderBy(es => es.Day)
                        .ThenBy(s => s.TimeStamp.Hour < 6 ? 1 : 0)
                        .ThenBy(s => s.TimeStamp)
                        .Select(s => new ScheduleInputModel
                    {
                        Day = s.Day,
                        TimeStamp = s.TimeStamp,
                        Label = s.Label
                    })
                ]
            };
        }
        else
        {
            Event = new EventModel
            {
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(1),
                DonationTypes = [],
                UploadSessionId = Guid.NewGuid().ToString()
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Event.EndDate < Event.StartDate)
        {
            ModelState.AddModelError("Event.EndDate", "End Date cannot be earlier than Start Date");
        }

        if (!ModelState.IsValid) return Page();

        Event? @event;
        var isNew = Event.Id == 0;

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
                .FirstOrDefaultAsync(e => e.Id == Event.Id);

            if (@event is null)
            {
                toastNotification.AddErrorToastMessage("Event not found");
                return RedirectToPage("/Admin/Events");
            }
        }

        @event.Name = Event.Name;
        @event.StartDate = Event.StartDate;
        @event.EndDate = Event.EndDate;
        @event.Location = Event.Location;
        @event.Description = Event.Description;
        @event.TravelInstructions = Event.TravelInstructions;
        @event.AccommodationCodes = Event.AccommodationCodes;
        @event.AccommodationDetails = Event.AccommodationDetails;
        @event.SaveDateTemplateId = Event.SaveDateEmailTemplateId;
        @event.InvitationTemplateId = Event.InvitationEmailTemplateId;
        @event.EmailWithLink = Event.SendWithLink;
        @event.MaxParticipantsPerRsvp = Event.MaxParticipantsPerRsvp;
        @event.IsFinalised = Event.IsFinalised;

        @event.DonationIban = Event.DonationTypes.Contains(DonationType.Iban) ? Event.DonationIban : null;
        @event.DonationLink = Event.DonationTypes.Contains(DonationType.Link) ? Event.DonationLink : null;
        @event.AllowPhysicalGifts = Event.DonationTypes.Contains(DonationType.Physical);

        // Booking Links
        @event.BookingLinks.Clear();
        foreach (var link in Event.BookingLinks.Where(l => !string.IsNullOrWhiteSpace(l)))
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
        var faqIndex = 1;
        foreach (var faq in Event.FaqItems.Where(f => !string.IsNullOrWhiteSpace(f.Question) && !string.IsNullOrWhiteSpace(f.Answer)))
        {
            @event.EventFaq.Add(new EventFaqItem { Question = faq.Question, Answer = faq.Answer, SortOrder = faqIndex++ });
        }

        // Schedule
        @event.EventSchedule.Clear();
        foreach (var step in Event.ScheduleSteps.Where(s => !string.IsNullOrWhiteSpace(s.Label) && s.Day <= Event.MaxEventDays()))
        {
            @event.EventSchedule.Add(new EventScheduleStep { Day = step.Day, TimeStamp = step.TimeStamp, Label = step.Label });
        }

        await db.SaveChangesAsync();

        if (isNew && !string.IsNullOrEmpty(Event.UploadSessionId))
        {
            await fileService.MoveEventImagesAsync(Event.UploadSessionId, @event.Id);

            // Update URLs in Description and TravelInstructions
            var tempPath = $"/events/{Event.UploadSessionId}/image/";
            var permanentPath = $"/events/{@event.Id}/image/";

            if (!string.IsNullOrEmpty(@event.Description))
                @event.Description = @event.Description.Replace(tempPath, permanentPath);

            if (!string.IsNullOrEmpty(@event.TravelInstructions))
                @event.TravelInstructions = @event.TravelInstructions.Replace(tempPath, permanentPath);

            await db.SaveChangesAsync();
        }

        var currentEventImages = @event.ExtractEventImageFileNames();
        await fileService.SyncEventImagesAsync(@event.Id, currentEventImages);

        toastNotification.AddSuccessToastMessage($"Event {(isNew ? "created" : "updated")} successfully!");

        if (!string.IsNullOrEmpty(RedirectAfterSave) && (Url.IsLocalUrl(RedirectAfterSave) || RedirectAfterSave.StartsWith('/')))
            return Redirect(RedirectAfterSave);

        return Page();
    }

    private static List<DonationType> GetDonationTypes(Event @event)
    {
        var types = new List<DonationType>();
        if (!string.IsNullOrEmpty(@event.DonationIban)) types.Add(DonationType.Iban);
        if (!string.IsNullOrEmpty(@event.DonationLink)) types.Add(DonationType.Link);
        if (@event.AllowPhysicalGifts) types.Add(DonationType.Physical);
        return types;
    }

    public class EventModel
    {
        public int Id { get; set; }
        public string? UploadSessionId { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Location { get; set; }
        public bool IsFinalised { get; set; } = false;
        [Required] public string Description { get; set; } = null!;
        public string? TravelInstructions { get; set; }

        [ModelBinder(BinderType = typeof(FlatListModelBinder))]
        public List<string> AccommodationCodes { get; set; } = [];

        public string? AccommodationDetails { get; set; }
        public string? SaveDateEmailTemplateId { get; set; }
        public bool SendWithLink { get; set; } = false;
        public string? InvitationEmailTemplateId { get; set; }
        public int MaxParticipantsPerRsvp { get; set; } = 4;
        public List<DonationType> DonationTypes { get; set; } = [];
        public string? DonationIban { get; set; }
        public string? DonationLink { get; set; }

        [ModelBinder(BinderType = typeof(FlatListModelBinder))]
        public List<string> BookingLinks { get; set; } = [];

        public List<FaqInputModel> FaqItems { get; set; } = [];
        public List<ScheduleInputModel> ScheduleSteps { get; set; } = [];

        public int MaxEventDays() => (EndDate - StartDate).Days + 1;
    }

    public class FaqInputModel
    {
        public string Question { get; set; } = null!;
        public string Answer { get; set; } = null!;
        public int SortOrder { get; set; }
    }

    public class ScheduleInputModel
    {
        public int Day { get; set; }
        public TimeOnly TimeStamp { get; set; }
        public string Label { get; set; } = null!;
    }

    public enum DonationType
    {
        Iban,
        Link,
        Physical
    }
}
