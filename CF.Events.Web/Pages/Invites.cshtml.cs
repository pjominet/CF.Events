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

        MyInvites = await db.UserEvents
            .Where(r => r.UserId == userId && r.Event.IsActive)
            .Include(r => r.Event)
            .Include(r => r.Rsvp)
            .Select(ue => new InviteRow(ue.Event, ue.Rsvp != null && ue.Rsvp.SubmittedAt > DateTime.UtcNow))
            .ToListAsync();

        return Page();
    }

    public record InviteRow(Event Event, bool HasRsvped);
}
