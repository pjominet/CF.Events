using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[Route("events")]
public class EventController(
    EventsDbContext db,
    MailjetService mailjetService,
    IToastNotification toastNotification,
    ILogger<EventController> logger,
    IWebHostEnvironment env) : Controller
{
    [HttpGet("{eventId:int}/asset")]
    public async Task<IActionResult> Get([FromRoute] int eventId)
    {
        var userId = User.GetId();
        var isInvited = await db.Rsvps.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
        if (!isInvited && !User.IsAdmin())
            return Forbid();

        var ev = await db.Events.FindAsync(eventId);
        if (ev is null || string.IsNullOrEmpty(ev.InvitationFileName))
            return NotFound();

        // The full path is built dynamically from the event ID (folder) and the
        // stored technical file name.
        var invitationsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Resources", "Invitations"));
        var requested = Path.GetFullPath(Path.Combine(invitationsRoot, eventId.ToString(), ev.InvitationFileName));

        // Prevent path traversal outside the invitations folder.
        if (!requested.StartsWith(invitationsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return Forbid();

        if (!System.IO.File.Exists(requested))
            return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(requested, out var contentType))
            contentType = "application/octet-stream";

        return PhysicalFile(requested, contentType);
    }

    [HttpPost("{eventId:int}/invite")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Get([FromRoute] int eventId, [FromBody] List<string> userIds)
    {
        var @event = await db.Events
            .Include(e => e.EventUsers)
            .ThenInclude(eu => eu.User)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (@event is null)
        {
            toastNotification.AddWarningToastMessage("Event not found");
            return RedirectToPage($"/admin/events/{eventId}");
        }

        foreach (var userid in userIds.Where(userid => @event.EventUsers.All(eu => eu.UserId != userid)))
        {
            @event.EventUsers.Add(new UserEvent
            {
                EventId = eventId,
                UserId = userid
            });
        }

        try
        {
            var count = await db.SaveChangesAsync();

            foreach (var userEvent in @event.EventUsers)
            {
                logger.LogInformation("Sending invitation to {Email}", userEvent.User.Email);
                await mailjetService.SendInvitationAsync(@event.Name, userEvent.User.DisplayName!, userEvent.User.Email!, @event.InviteCode);
            }

            toastNotification.AddSuccessToastMessage($"Successfully created {count} invitations");
            return RedirectToPage($"/admin/events/{eventId}");
        }
        catch
        {
            toastNotification.AddErrorToastMessage("Invitations could not be created");
            return RedirectToPage($"/admin/events/{eventId}");
        }
    }
}
