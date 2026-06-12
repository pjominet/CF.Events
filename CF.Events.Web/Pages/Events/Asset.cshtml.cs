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
    public async Task<IActionResult> OnGetAsync(int eventId, string? assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isInvited = await db.Rsvps.AnyAsync(r => r.EventId == eventId && r.UserId == userId);
        if (!isInvited && !User.IsInRole(Constants.Roles.Admin))
            return Forbid();

        var ev = await db.Events.FindAsync(eventId);
        if (ev is null || string.IsNullOrEmpty(ev.InvitationFileName))
            return NotFound();

        var designRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "Resources", "Invitations", ev.InvitationFileName));
        var requested = Path.GetFullPath(Path.Combine(designRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));

        // Prevent path traversal outside the design folder.
        if (!requested.StartsWith(designRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
            !requested.Equals(designRoot, StringComparison.Ordinal))
            return Forbid();

        if (!System.IO.File.Exists(requested))
            return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(requested, out var contentType))
            contentType = "application/octet-stream";

        return PhysicalFile(requested, contentType);
    }
}
