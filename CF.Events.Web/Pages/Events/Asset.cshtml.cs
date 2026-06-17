using System.Security.Claims;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Pages.Events;

[Authorize]
public class AssetModel(EventsDbContext db, IWebHostEnvironment env) : PageModel
{
    public async Task<IActionResult> OnGetAsync(int eventId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isInvited = await db.Rsvps.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
        if (!isInvited && !User.IsInRole(Constants.Roles.Admin))
            return Forbid();

        var ev = await db.Events.FindAsync(eventId);
        if (ev is null || string.IsNullOrEmpty(ev.InvitationFileName))
            return NotFound();

        // The full path is built dynamically from the event Id (folder) and the
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
}
