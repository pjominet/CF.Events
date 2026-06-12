using System.Security.Claims;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Pages.Events;

[Authorize]
public class InvitationModel(EventsDbContext db, IWebHostEnvironment env) : PageModel
{
    public Event? EventData { get; private set; }
    public HtmlString? ProcessedHtml { get; private set; }
    public bool NotFoundOrForbidden { get; private set; }

    public async Task<IActionResult> OnGetAsync(int eventId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isInvited = await db.Rsvps.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
        if (!isInvited && !User.IsInRole(Constants.Roles.Admin))
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

        if (!string.IsNullOrEmpty(EventData.InvitationFileName))
            ProcessedHtml = await LoadInvitationHtmlAsync(EventData);

        return Page();
    }

    private async Task<HtmlString> LoadInvitationHtmlAsync(Event ev)
    {
        var invitationsPath = Path.Combine(env.ContentRootPath, "Resources", "Invitations");
        var filePath = Path.Combine(invitationsPath, ev.InvitationFileName!, "index.html");

        if (!System.IO.File.Exists(filePath))
            return new HtmlString("<div class='alert alert-warning'>Invitation content not found.</div>");

        var rawHtml = await System.IO.File.ReadAllTextAsync(filePath);

        var processed = rawHtml
            .Replace("[EventDate]", ev.Date.ToString("MMMM dd, yyyy"))
            .Replace("[EventLocation]", ev.Location ?? "To be announced")
            .Replace("[EventName]", ev.Name);

        // Rewrite relative asset references to the authorized asset endpoint.
        processed = processed.Replace($"invitations/{ev.InvitationFileName}/", $"/events/{ev.Id}/asset/");

        return new HtmlString(processed);
    }
}
