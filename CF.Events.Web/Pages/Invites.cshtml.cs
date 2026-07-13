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
    public const int PageSize = 9;
    public int PageNumber { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(int pageNumber = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Challenge();

        PageNumber = pageNumber;

        var query = db.EventUsers
            .Where(r => r.UserId == userId && r.Event.IsActive)
            .OrderBy(r => r.Event.StartDate);

        TotalCount = await query.CountAsync();

        MyInvites = await query
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .Select(ue => new InviteRow(
                ue.Event,
                ue.Rsvp != null && ue.Rsvp.SubmittedAt <= DateTime.UtcNow,
                ue.Rsvp != null && ue.Rsvp.SubmittedAt <= DateTime.UtcNow && ue.Rsvp.Attending,
                ue.Rsvp != null ? ue.Rsvp.AttendanceDays : new Dictionary<int, int>()))
            .ToListAsync();

        return Page();
    }

    public record InviteRow(Event Event, bool HasRsvped, bool Attending, Dictionary<int, int> AttendanceDays);
}
