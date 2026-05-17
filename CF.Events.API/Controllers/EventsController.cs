using CF.Events.API.Data;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static CF.Events.Shared.Constants;

namespace CF.Events.API.Controllers;

[Route("events")]
public class EventsController(EventsDbContext db) : ApiController
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyEvents()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        // Get all events where the user has an RSVP or is invited (for now, let's just use RSVPs as proxy for "invited")
        // In a real system we'd have an Invite table. The prompt says "if they log in they can see the invite in their account".
        // Let's assume an RSVP with Attending=false or null initially means "Invited but not yet responded".
        // But the prompt also says "admins can create invites".
        // Let's use the Rsvp table as the Invite table since it links User and Event.

        var myRsvps = await db.Rsvps
            .Where(r => r.UserId == userId)
            .Join(db.Events, r => r.EventId, e => e.Id, (r, e) => new { Rsvp = r, Event = e })
            .ToListAsync();

        return Ok(myRsvps);
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

    // Admin endpoints
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateEvent(Event ev)
    {
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEvent), new { id = ev.Id }, ev);
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
