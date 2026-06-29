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
    public Event? EventData { get; private set; }
    public bool HasResponded { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int eventId)
    {
        var userId = User.GetId();
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (rsvp is null && !User.IsAdmin())
            return Redirect("/");

        EventData = await db.Events
            .Include(e => e.EventConfig)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (EventData is null) return Redirect("/");

        if (rsvp is null) return Page();

        HasResponded = rsvp.SubmittedAt > DateTime.MinValue.AddDays(1);
        Input.Attending = rsvp.Attending;
        Input.BringsPlusOne = rsvp.BringsPlusOne;
        Input.BringsKids = rsvp.BringsKids;
        Input.JoinsForDinner = rsvp.JoinsForDinner;
        Input.JoinsForLunch = rsvp.JoinsForLunch;
        Input.JoinsForBreakfast = rsvp.JoinsForBreakfast;
        Input.JoinsForBrunch = rsvp.JoinsForBrunch;
        Input.NeedsAccommodation = rsvp.NeedsAccommodation;
        Input.Comments = rsvp.Comments;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int eventId)
    {
        var userId = User.GetId();
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (rsvp is null)
        {
            toastNotification.AddWarningToastMessage("You are not invited to this event.");
            return Redirect("/");
        }

        var eventConfig = await db.EventConfigs.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (eventConfig is null)
        {
            toastNotification.AddWarningToastMessage("Event is missing configuration data");
            return Page();
        }

        rsvp.Attending = Input.Attending;
        rsvp.SubmittedAt = DateTime.UtcNow;
        if (Input.Attending)
        {
            rsvp.BringsPlusOne = eventConfig.AllowPartners && Input.BringsPlusOne == true;
            rsvp.BringsKids = eventConfig.AllowKids && Input.BringsKids == true;
            rsvp.JoinsForDinner = eventConfig.OfferDinner && Input.JoinsForDinner == true;
            rsvp.JoinsForLunch = eventConfig.OfferLunch && Input.JoinsForLunch == true;
            rsvp.JoinsForBreakfast = eventConfig.OfferBreakfast && Input.JoinsForBreakfast == true;
            rsvp.JoinsForBrunch = eventConfig.OfferBrunch && Input.JoinsForBrunch == true;
            rsvp.NeedsAccommodation = eventConfig.ShowAccommodationOptions && Input.NeedsAccommodation == true;
            rsvp.Comments = eventConfig.AllowComments ? Input.Comments : null;
        }

        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Thank you for your response!");
        return Redirect("/");
    }

    public sealed class InputModel
    {
        public bool Attending { get; set; } = true;
        public bool? BringsPlusOne { get; set; }
        public bool? BringsKids { get; set; }
        public bool? JoinsForDinner { get; set; }
        public bool? JoinsForLunch { get; set; }
        public bool? JoinsForBreakfast { get; set; }
        public bool? JoinsForBrunch { get; set; }
        public bool? NeedsAccommodation { get; set; }
        [StringLength(500)]
        public string? Comments { get; set; }
    }
}
