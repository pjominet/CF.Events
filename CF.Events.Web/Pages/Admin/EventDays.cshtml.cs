using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Constants.Roles.Admin)]
public class EventDaysModel(EventsDbContext db) : PageModel
{
    public Event? EventData { get; private set; }
    public List<EventDay> Days { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        EventData = await db.Events.FindAsync(id);
        if (EventData is null)
            return NotFound();

        Days = await db.EventDays
            .Where(d => d.EventId == id)
            .OrderBy(d => d.Date)
            .ToListAsync();

        return Page();
    }
}
