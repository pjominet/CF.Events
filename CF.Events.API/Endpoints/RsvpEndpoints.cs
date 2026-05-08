using CF.Events.API.Data;
using CF.Events.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.API.Endpoints;

public static class RsvpEndpoints
{
    public static void MapRsvpEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events/engagement/rsvp");

        group.MapPost("/", async (Rsvp rsvp, EventsDbContext db) =>
        {
            // Check if fingerprint already exists
            var existing = await db.Rsvps.FirstOrDefaultAsync(r => r.Fingerprint == rsvp.Fingerprint);
            if (existing != null)
                return Results.Conflict("An RSVP already exists for this fingerprint.");

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
            return Results.Created($"/api/events/engagement/rsvp/{rsvp.Id}", rsvp);
        });

        group.MapGet("/check/{fingerprint}", async (string fingerprint, EventsDbContext db) =>
        {
            var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.Fingerprint == fingerprint);
            return rsvp != null ? Results.Ok(rsvp) : Results.NotFound();
        });

        group.MapGet("/code/{code}", async (string code, EventsDbContext db) =>
        {
            var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.AccessCode == code.ToUpper());
            return rsvp != null ? Results.Ok(rsvp) : Results.NotFound();
        });

        group.MapPut("/{id:int}", async (int id, Rsvp updatedRsvp, EventsDbContext db) =>
        {
            var rsvp = await db.Rsvps.FindAsync(id);
            if (rsvp == null)
                return Results.NotFound();

            if (rsvp.Fingerprint != updatedRsvp.Fingerprint)
                return Results.Forbid();

            rsvp.Name = updatedRsvp.Name;
            rsvp.Attending = updatedRsvp.Attending;
            rsvp.BringsPlusOne = updatedRsvp.BringsPlusOne;
            rsvp.JoinForDinner = updatedRsvp.JoinForDinner;
            rsvp.Comments = updatedRsvp.Comments;

            await db.SaveChangesAsync();
            return Results.Ok(rsvp);
        });

        group.MapDelete("/{id:int}", async (int id, string fingerprint, EventsDbContext db) =>
        {
            var rsvp = await db.Rsvps.FindAsync(id);
            if (rsvp == null)
                return Results.NotFound();

            if (rsvp.Fingerprint != fingerprint)
                return Results.Forbid();

            db.Rsvps.Remove(rsvp);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/admin/{id:int}", async (int id, EventsDbContext db) =>
        {
            var rsvp = await db.Rsvps.FindAsync(id);
            if (rsvp == null)
                return Results.NotFound();

            db.Rsvps.Remove(rsvp);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapGet("/", async (EventsDbContext db) => Results.Ok((object?)await db.Rsvps.ToListAsync())).RequireAuthorization();
    }
}
