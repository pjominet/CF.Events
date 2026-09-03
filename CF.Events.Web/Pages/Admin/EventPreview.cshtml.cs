using CF.Events.Web.Data;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Roles.Admin)]
public class EventPreviewModel(EventsDbContext db, IToastNotification toastNotification) : PageModel
{
    public Event Event { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var @event = await db.Events
            .Include(e => e.EventFaq)
            .Include(e => e.EventSchedule)
            .Include(e => e.BookingLinks)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (@event is null)
        {
            toastNotification.AddErrorToastMessage("Event not found");
            return RedirectToPage("/Admin/Events");
        }

        Event = @event;
        return Page();
    }
}
