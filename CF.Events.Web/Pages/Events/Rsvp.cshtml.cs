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

        EventData = await db.Events.FindAsync(eventId);
        if (EventData is null)
            return Redirect("/");

        if (rsvp is null) return Page();

        HasResponded = rsvp.SubmittedAt > DateTime.MinValue.AddDays(1);
        Input.Attending = rsvp.Attending;
        Input.BringsPlusOne = rsvp.BringsPlusOne;
        Input.JoinForDinner = rsvp.JoinsForDinner;
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

        rsvp.Attending = Input.Attending;
        rsvp.BringsPlusOne = Input is { Attending: true, BringsPlusOne: true };
        rsvp.JoinsForDinner = Input is { Attending: true, JoinForDinner: true };
        rsvp.Comments = Input.Comments;
        rsvp.SubmittedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Thank you for your response!");
        return Redirect("/");
    }

    public sealed class InputModel
    {
        public bool Attending { get; set; } = true;
        public bool BringsPlusOne { get; set; }
        public bool JoinForDinner { get; set; }
        [StringLength(500)]
        public string? Comments { get; set; }
    }
}
