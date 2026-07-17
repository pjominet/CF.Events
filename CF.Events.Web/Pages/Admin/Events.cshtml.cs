using CF.Events.Web.Data;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Roles.Admin)]
public class EventsModel(
    EventsDbContext db,
    IFileService fileService,
    IToastNotification toastNotification) : PageModel
{
    public List<Event> AllEvents { get; private set; } = [];

    public Dictionary<int, int> InviteeCounts { get; private set; } = [];

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var @event = await db.Events.FindAsync(id);
        if (@event is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage();
        }

        @event.IsActive = !@event.IsActive;
        await db.SaveChangesAsync();
        toastNotification.AddSuccessToastMessage($"Event {(@event.IsActive ? "activated" : "deactivated")} successfully");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var @event = await db.Events.FindAsync(id);
        if (@event is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage();
        }

        var rsvps = await db.Rsvps.Where(r => r.EventId == id).ToListAsync();
        db.Rsvps.RemoveRange(rsvps);
        db.Events.Remove(@event);
        await db.SaveChangesAsync();

        await fileService.DeleteEventImagesAsync(@event.Id);

        toastNotification.AddSuccessToastMessage("Event deleted successfully");
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        AllEvents = await db.Events
            .Include(e => e.BookingLinks)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync();

        InviteeCounts = await db.EventUsers
            .GroupBy(r => r.EventId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }
}
