using System.Security.Claims;
using CF.Events.Web.Data;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Pages;

[Authorize]
public class InvitesModel(EventsDbContext db) : PageModel
{
    public List<InviteRow> MyInvites { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Challenge();

        MyInvites = await db.Rsvps
            .Where(r => r.UserId == userId)
            .Join(db.Events, r => r.EventId, e => e.Id, (r, e) => new { Rsvp = r, Event = e })
            .Where(x => x.Event.IsActive)
            .Select(x => new InviteRow(x.Event, x.Rsvp))
            .ToListAsync();

        // Convenience: if the user has exactly one invitation, go straight to it.
        if (MyInvites.Count == 1)
            return Redirect($"/events/{MyInvites[0].Event.Id}/invitation");

        return Page();
    }

    public record InviteRow(Event Event, Rsvp Rsvp);
}
