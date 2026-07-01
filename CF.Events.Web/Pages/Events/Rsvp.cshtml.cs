using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Pages.Events;

[Authorize]
public class RsvpModel(EventsDbContext db) : PageModel
{
    public int InvitationId { get; private set; }
    public Event? EventData { get; private set; }
    public bool NotFoundOrForbidden { get; private set; }

    public async Task<IActionResult> OnGetAsync(int eventId)
    {
        var userId = User.GetId();

        // Find the user's invitation for this event
        var invitation = await db.InvitedPersons
            .Where(ip => ip.Invitation.EventId == eventId && ip.UserId == userId)
            .Select(ip => new { ip.InvitationId, ip.Invitation.Event })
            .FirstOrDefaultAsync();

        if (invitation is null)
        {
            NotFoundOrForbidden = true;
            return Page();
        }

        InvitationId = invitation.InvitationId;
        EventData = invitation.Event;

        return Page();
    }
}
