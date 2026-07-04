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
    public string? AssignedAccommodationCode { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

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
        if (rsvp is null)
        {
            toastNotification.AddWarningToastMessage("You are not invited to this event");
            return Redirect("/");
        }

        EventData = await db.Events.FirstAsync(e => e.Id == eventId);

        AssignedAccommodationCode = userEvent?.AssignedAccommodationCode;
        HasResponded = rsvp.SubmittedAt > DateTime.MinValue.AddDays(1);
        Input.Attending = rsvp.Attending;
        Input.CommonDietaryOptions = rsvp.CommonDietaryOptions;
        Input.OtherDietaryDetails = rsvp.OtherDietaryDetails;
        Input.Comments = rsvp.Comments;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int eventId)
    {
        var userId = User.GetId();

        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (rsvp is null)
        {
            toastNotification.AddWarningToastMessage("You are not invited to this event");
            return Redirect("/");
        }

        rsvp.Attending = Input.Attending;
        rsvp.SubmittedAt = DateTime.UtcNow;
        if (Input.Attending)
        {
            rsvp.CommonDietaryOptions = Input.CommonDietaryOptions;
            rsvp.OtherDietaryDetails = Input.OtherDietaryDetails;
            rsvp.Comments = Input.Comments;
        }

        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Thank you for your response!");
        return Redirect("/");
    }

    public sealed class InputModel
    {
        public bool Attending { get; set; } = true;
        public int? AccommodationDuration { get; set; }
        public List<int> SelectedDays { get; set; } = [];

        public DietaryOptions[]? CommonDietaryOptions { get; set; }
        public string? OtherDietaryDetails { get; set; }

        [StringLength(500)]
        public string? Comments { get; set; }
    }
}
