using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace CF.Events.Web.Pages.Events;

[Authorize]
public class RsvpModel(EventsDbContext db, IToastNotification toastNotification) : PageModel
{
    public required Event EventData { get; set; }
    public bool HasResponded { get; private set; }
    public bool RespondedAttending { get; private set; }
    public string? AssignedAccommodationCode { get; private set; }

    [BindProperty]
    public InputModel NewRsvp { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int eventId)
    {
        var userId = User.GetId();

        var userEvent = await db.EventUsers.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (userEvent is null && !User.IsAdmin())
        {
            toastNotification.AddWarningToastMessage("You are not invited to this event");
            return Redirect("/");
        }

        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

        EventData = await db.Events
            .Include(e => e.BookingLinks)
            .FirstAsync(e => e.Id == eventId);

        AssignedAccommodationCode = userEvent?.AssignedAccommodationCode;
        HasResponded = rsvp?.SubmittedAt > DateTime.MinValue.AddDays(1);
        RespondedAttending = rsvp?.Attending ?? false;

        if (rsvp is not null)
        {
            NewRsvp = new InputModel
            {
                Attending = rsvp.Attending,
                AttendanceDays = rsvp.AttendanceDays,
                DietaryOptionNbrPeople = rsvp.DietaryOptionNbrPeople,
                CommonDietaryOptions = rsvp.CommonDietaryOptions,
                OtherDietaryDetails = rsvp.OtherDietaryDetails,
                Comments = rsvp.Comments
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int eventId)
    {
        var userId = User.GetId();

        var rsvp = await db.Rsvps
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

        if (rsvp is null)
        {
            rsvp = new Rsvp { EventId = eventId, UserId = userId };
            db.Rsvps.Add(rsvp);
        }

        rsvp.Attending = NewRsvp.Attending;
        rsvp.SubmittedAt = DateTime.UtcNow;
        if (NewRsvp.Attending)
        {
            rsvp.AttendanceDays = NewRsvp.AttendanceDays;
            rsvp.DietaryOptionNbrPeople = NewRsvp.DietaryOptionNbrPeople;
            rsvp.CommonDietaryOptions = NewRsvp.CommonDietaryOptions;
            rsvp.OtherDietaryDetails = NewRsvp.OtherDietaryDetails;
            rsvp.Comments = NewRsvp.Comments;
        }

        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Thank you for your response!");
        return Redirect("/");
    }

    public async Task<IActionResult> OnPostCancelAsync(int eventId)
    {
        var userId = User.GetId();

        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (rsvp is null)
        {
            toastNotification.AddWarningToastMessage("You are not invited to this event");
            return Redirect("/");
        }

        rsvp.Attending = true;
        rsvp.SubmittedAt = DateTime.MinValue;
        rsvp.AttendanceDays = [];
        rsvp.DietaryOptionNbrPeople = 0;
        rsvp.CommonDietaryOptions = [];
        rsvp.OtherDietaryDetails = null;
        rsvp.Comments = null;

        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Your RSVP has been cancelled. You can submit a new response.");
        return RedirectToPage(new { eventId });
    }

    public bool HasAccommodationInfo()
    {
        return !string.IsNullOrWhiteSpace(AssignedAccommodationCode)
               || !string.IsNullOrWhiteSpace(EventData.AccommodationDetails)
               || EventData.AccommodationCodes.Count > 0;
    }

    public sealed class InputModel
    {
        public bool Attending { get; set; } = true;
        public Dictionary<int, int> AttendanceDays { get; set; } = new (){{ 1, 1 }};

        public int DietaryOptionNbrPeople { get; set; }
        public List<DietaryOptions> CommonDietaryOptions { get; set; } = [];
        public string? OtherDietaryDetails { get; set; }

        [StringLength(500)]
        public string? Comments { get; set; }
    }
}
