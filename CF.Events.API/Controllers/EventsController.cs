using CF.Events.API.Data;
using CF.Events.Shared.DTOs;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CF.Events.API.Infrastructure.Attributes;
using static CF.Events.Shared.Constants;

namespace CF.Events.API.Controllers;

[ApiRoute("events")]
public class EventsController(EventsDbContext db, IWebHostEnvironment env) : ApiController
{
    private string GetInvitationsRootPath()
    {
        return Path.Combine(env.ContentRootPath, "Resources", "Invitations");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyEvents()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var myRsvps = await db.Rsvps
            .Where(r => r.UserId == userId)
            .Join(db.Events, r => r.EventId, e => e.Id, (r, e) => new { Rsvp = r, Event = e })
            .Where(x => x.Event.IsActive)
            .ToListAsync();

        return Ok(myRsvps);
    }

    [HttpGet("all")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetAllEvents()
    {
        var events = await db.Events.ToListAsync();
        return Ok(events);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetEvent(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);
        if (rsvp is null && !User.IsInRole(Roles.Admin)) return Forbid();

        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();

        return Ok(new { Event = ev, Rsvp = rsvp });
    }

    [HttpPost("{id}/rsvp")]
    [Authorize]
    public async Task<IActionResult> UpsertRsvp(int id, Rsvp rsvpRequest)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var existingRsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);
        if (existingRsvp is null) return NotFound("You are not invited to this event.");

        existingRsvp.Attending = rsvpRequest.Attending;
        existingRsvp.BringsPlusOne = rsvpRequest.BringsPlusOne;
        existingRsvp.JoinForDinner = rsvpRequest.JoinForDinner;
        existingRsvp.Comments = rsvpRequest.Comments;
        existingRsvp.SubmittedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(existingRsvp);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateEvent(Event ev)
    {
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEvent), new { id = ev.Id }, ev);
    }

    [HttpGet("invitation-files")]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult GetInvitationFiles()
    {
        var invitationsPath = GetInvitationsRootPath();
        if (!Directory.Exists(invitationsPath)) return Ok(Array.Empty<string>());

        var folders = Directory.GetDirectories(invitationsPath)
            .Select(Path.GetFileName)
            .Where(f => System.IO.File.Exists(Path.Combine(invitationsPath, f!, "index.html")))
            .ToList();

        return Ok(folders);
    }

    [HttpGet("{id}/invitation-content")]
    [Authorize]
    public async Task<IActionResult> GetInvitationContent(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var rsvp = await db.Rsvps.AnyAsync(r => r.EventId == id && r.UserId == userId);
        if (!rsvp && !User.IsInRole(Roles.Admin)) return Forbid();

        var ev = await db.Events.FindAsync(id);
        if (ev is null || string.IsNullOrEmpty(ev.InvitationFileName)) return NotFound();

        var invitationsPath = GetInvitationsRootPath();
        var filePath = Path.Combine(invitationsPath, ev.InvitationFileName, "index.html");

        if (!System.IO.File.Exists(filePath)) return NotFound();

        var rawHtml = await System.IO.File.ReadAllTextAsync(filePath);

        // Basic placeholder replacement
        var processedHtml = rawHtml
            .Replace("[EventDate]", ev.Date.ToString("MMMM dd, yyyy"))
            .Replace("[EventLocation]", ev.Location ?? "To be announced")
            .Replace("[EventName]", ev.Name);

        // Inject API base URL for assets
        var baseUrl = $"{Request?.Scheme ?? "http"}://{Request?.Host.Value ?? "localhost"}{Request?.PathBase.Value ?? ""}/api/events/invitation-assets/{ev.InvitationFileName}";
        processedHtml = processedHtml.Replace("invitations/" + ev.InvitationFileName + "/", baseUrl + "/");

        return Ok(new InvitationContentDto { HtmlContent = processedHtml });
    }

    [HttpGet("invitation-assets/{designName}/{*assetPath}")]
    [Authorize]
    public IActionResult GetInvitationAsset(string designName, string assetPath)
    {
        // For simplicity, we allow public access to assets, but they are not easily discoverable
        // without knowing the designName and assetPath.
        var invitationsPath = GetInvitationsRootPath();
        var filePath = Path.Combine(invitationsPath, designName, assetPath.Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(filePath)) return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(filePath, contentType);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateEvent(int id, Event updatedEvent)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();

        ev.Name = updatedEvent.Name;
        ev.Type = updatedEvent.Type;
        ev.Date = updatedEvent.Date;
        ev.Description = updatedEvent.Description;
        ev.Location = updatedEvent.Location;
        ev.InvitationFileName = updatedEvent.InvitationFileName;
        ev.IsActive = updatedEvent.IsActive;

        await db.SaveChangesAsync();
        return Ok(ev);
    }

    [HttpPost("{id}/invite")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> InviteUser(int id, [FromBody] string email)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound("Event not found");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return NotFound("User not found");

        var existing = await db.Rsvps.AnyAsync(r => r.EventId == id && r.UserId == user.Id);
        if (existing) return BadRequest("User already invited");

        var rsvp = new Rsvp
        {
            EventId = id,
            UserId = user.Id,
            Attending = false // Initially false/not-responded
        };

        db.Rsvps.Add(rsvp);
        await db.SaveChangesAsync();
        return Ok();
    }
}
