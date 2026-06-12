using System.Security.Claims;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Pages.Events;

[Authorize]
public class RsvpModel(EventsDbContext db) : PageModel
{
    public Event? EventData { get; private set; }
    public bool HasResponded { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int eventId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Challenge();

        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (rsvp is null && !User.IsInRole(Constants.Roles.Admin))
            return Redirect("/");

        EventData = await db.Events.FindAsync(eventId);
        if (EventData is null)
            return Redirect("/");

        if (rsvp is not null)
        {
            HasResponded = rsvp.SubmittedAt > DateTime.MinValue.AddDays(1);
            Input.Attending = rsvp.Attending;
            Input.BringsPlusOne = rsvp.BringsPlusOne;
            Input.JoinForDinner = rsvp.JoinForDinner;
            Input.Comments = rsvp.Comments;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int eventId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Challenge();

        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (rsvp is null)
        {
            TempData["Toast"] = "You are not invited to this event.";
            TempData["ToastType"] = "error";
            return Redirect("/");
        }

        rsvp.Attending = Input.Attending;
        rsvp.BringsPlusOne = Input.Attending && Input.BringsPlusOne;
        rsvp.JoinForDinner = Input.Attending && Input.JoinForDinner;
        rsvp.Comments = Input.Comments;
        rsvp.SubmittedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        TempData["Toast"] = "Thank you for your response!";
        TempData["ToastType"] = "success";
        return Redirect("/");
    }

    public sealed class InputModel
    {
        public bool Attending { get; set; } = true;
        public bool BringsPlusOne { get; set; }
        public bool JoinForDinner { get; set; }
        [System.ComponentModel.DataAnnotations.StringLength(500)]
        public string? Comments { get; set; }
    }
}
