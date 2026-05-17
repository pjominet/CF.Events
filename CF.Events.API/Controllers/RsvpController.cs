using CF.Events.API.Data;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using static CF.Events.Shared.Constants;

namespace CF.Events.API.Controllers;

[Route("events/engagement/rsvp")]
[EnableRateLimiting(RateLimiting.Fixed)]
public class RsvpController(EventsDbContext db) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> CreateRsvp(Rsvp rsvp)
    {
        // Generate unique access code
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        string code;
        do
        {
            code = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        } while (await db.Rsvps.AnyAsync(r => r.AccessCode == code));

        rsvp.AccessCode = code;
        db.Rsvps.Add(rsvp);
        await db.SaveChangesAsync();
        return Created($"/api/events/engagement/rsvp/{rsvp.Id}", rsvp);
    }

    [HttpGet("code/{code}")]
    [EnableRateLimiting(RateLimiting.Strict)]
    public async Task<IActionResult> GetByCode(string code)
    {
        var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.AccessCode == code.ToUpper());
        return rsvp is not null ? Ok(rsvp) : NotFound();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRsvp(int id, Rsvp updatedRsvp)
    {
        var rsvp = await db.Rsvps.FindAsync(id);
        if (rsvp is null)
            return NotFound();

        if (rsvp.AccessCode != updatedRsvp.AccessCode)
            return Forbid();

        rsvp.Name = updatedRsvp.Name;
        rsvp.Attending = updatedRsvp.Attending;
        rsvp.BringsPlusOne = updatedRsvp.BringsPlusOne;
        rsvp.JoinForDinner = updatedRsvp.JoinForDinner;
        rsvp.Comments = updatedRsvp.Comments;

        await db.SaveChangesAsync();
        return Ok(rsvp);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRsvp(int id, string accessCode)
    {
        var rsvp = await db.Rsvps.FindAsync(id);
        if (rsvp is null)
            return NotFound();

        if (!rsvp.AccessCode.Equals(accessCode, StringComparison.CurrentCultureIgnoreCase))
            return Forbid();

        db.Rsvps.Remove(rsvp);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("admin/{id:int}")]
    public async Task<IActionResult> AdminDeleteRsvp(int id)
    {
        var rsvp = await db.Rsvps.FindAsync(id);
        if (rsvp is null)
            return NotFound();

        db.Rsvps.Remove(rsvp);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> GetAllRsvps()
    {
        return Ok(await db.Rsvps.ToListAsync());
    }
}
