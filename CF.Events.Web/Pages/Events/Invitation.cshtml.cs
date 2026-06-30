using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Pages.Events;

[Authorize]
public class InvitationModel(EventsDbContext db) : PageModel
{
    public Event? EventData { get; private set; }
    public string? ImageUrl { get; private set; }
    public string BackUrl { get; private set; } = "/";
    public bool NotFoundOrForbidden { get; private set; }

    public async Task<IActionResult> OnGetAsync(int eventId)
    {
        var userId = User.GetId();
        var isInvited = await db.EventUsers.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
        var isAdmin = User.IsAdmin();
        if (!isInvited && !isAdmin)
        {
            NotFoundOrForbidden = true;
            return Page();
        }

        EventData = await db.Events.FindAsync(eventId);
        if (EventData is null)
        {
            NotFoundOrForbidden = true;
            return Page();
        }

        // Admins reach this page by previewing from the events list, regular users from their invitation list.
        BackUrl = isAdmin && !isInvited ? "/admin/events" : "/invites";

        if (!string.IsNullOrEmpty(EventData.InvitationFileName))
            ImageUrl = $"/events/{EventData.Id}/asset";

        return Page();
    }
}
