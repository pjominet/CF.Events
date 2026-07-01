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
    public int TotalCount { get; private set; }
    public int PageSize { get; } = 9;
    public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(int pageNumber = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Challenge();

        PageNumber = pageNumber;

        // Get all invited persons for this user, then join with invitations and events
        var query = db.InvitedPersons
            .Where(ip => ip.UserId == userId && ip.Invitation.Event.IsActive)
            .Include(ip => ip.Invitation)
                .ThenInclude(i => i.Event)
            .Include(ip => ip.Invitation)
                .ThenInclude(i => i.Rsvp)
            .OrderBy(ip => ip.Invitation.Event.Date);

        TotalCount = await query.CountAsync();

        MyInvites = await query
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .Select(ip => new InviteRow(
                ip.Invitation.Event,
                ip.Invitation.Rsvp != null && ip.Invitation.Rsvp.SubmittedAt > DateTime.UtcNow))
            .ToListAsync();

        return Page();
    }

    public record InviteRow(Event Event, bool HasRsvped);
}
