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
    public required EventConfig EventConfig { get; set; }
    public bool HasResponded { get; private set; }
    public string? AssignedAccommodationCode { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int eventId)
    {
        var userId = User.GetId();
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var userEvent = await db.EventUsers.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (userEvent is null && !User.IsAdmin())
            return Redirect("/");

        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        EventData = await db.Events.FirstAsync(e => e.Id == eventId);
        EventConfig = await db.EventConfigs.FirstAsync(e => e.EventId == eventId);

        if (rsvp is null) return Page();

        AssignedAccommodationCode = userEvent?.AssignedAccommodationCode;
        HasResponded = rsvp.SubmittedAt > DateTime.MinValue.AddDays(1);
        Input.Attending = rsvp.Attending;
        Input.BringsPlusOne = rsvp.BringsPlusOne == true;
        Input.BringsKids = rsvp.BringsKids == true;
        Input.NeedsAccommodation = rsvp.NeedsAccommodation == true;
        Input.AccommodationDuration = rsvp.AccommodationDuration;
        Input.CommonDietaryOptions = rsvp.CommonDietaryOptions;
        Input.OtherDietaryDetails = rsvp.OtherDietaryDetails;
        Input.Comments = rsvp.Comments;
        Input.KidsDetails = rsvp.KidsDetails ?? new Dictionary<KidAgeBracket, int>();

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
            rsvp.BringsPlusOne = eventConfig.AllowPartners && Input.BringsPlusOne;
            rsvp.BringsKids = eventConfig.AllowKids && Input.BringsKids;

            rsvp.NeedsAccommodation = eventConfig.ShowAccommodationOptions && Input.NeedsAccommodation;
            rsvp.AccommodationDuration = rsvp.NeedsAccommodation == true ? Input.AccommodationDuration : null;

            var offersFood = eventConfig.ShowFoodOptions;
            rsvp.CommonDietaryOptions = offersFood ? Input.CommonDietaryOptions : null;
            rsvp.OtherDietaryDetails = offersFood ? Input.OtherDietaryDetails : null;

            rsvp.KidsDetails = rsvp.BringsKids == true ? Input.KidsDetails : null;

            rsvp.Comments = eventConfig.AllowComments ? Input.Comments : null;
        }

        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Thank you for your response!");
        return Redirect("/");
    }

    public sealed class InputModel
    {
        public bool Attending { get; set; } = true;
        public bool BringsPlusOne { get; set; }
        public bool BringsKids { get; set; }
        public bool NeedsAccommodation { get; set; }
        public int? AccommodationDuration { get; set; }
        public List<int> SelectedDays { get; set; }

        public DietaryOptions[]? CommonDietaryOptions { get; set; }
        public string? OtherDietaryDetails { get; set; }

        public Dictionary<KidAgeBracket, int> KidsDetails { get; set; } = new();

        [StringLength(500)]
        public string? Comments { get; set; }
    }
}
