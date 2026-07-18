using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Pages.Events;

public class SaveTheDate(EventsDbContext db) : PageModel
{
    public bool NotFoundOrForbidden { get; private set; }
    public string? ImageUrl { get; private set; }

    public async Task<IActionResult> OnGet(int eventId, string userId)
    {
        var isInvited = await db.EventUsers.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
        var isAdmin = User.IsAdmin();
        if (!isInvited && !isAdmin)
            NotFoundOrForbidden = true;

        ImageUrl = $"/events/{eventId}/{userId}/asset?type=std";

        return Page();
    }
}
