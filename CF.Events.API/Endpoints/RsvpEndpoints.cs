using CF.Events.API.Data;
using CF.Events.Shared;
using CF.Events.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.API.Endpoints;

public static class RsvpEndpoints
{
    public static void MapRsvpEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events/engagement/rsvp").RequireRateLimiting(Constants.RateLimiting.Fixed);

        group.MapPost("/", async (Rsvp rsvp, EventsDbContext db) =>
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
            return Results.Created($"/api/events/engagement/rsvp/{rsvp.Id}", rsvp);
        });


        group.MapGet("/code/{code}", async (string code, EventsDbContext db) =>
        {
            var rsvp = await db.Rsvps.FirstOrDefaultAsync(r => r.AccessCode.Equals(code, StringComparison.CurrentCultureIgnoreCase));
            return rsvp is not null ? Results.Ok(rsvp) : Results.NotFound();
        }).RequireRateLimiting(Constants.RateLimiting.Strict);

        group.MapPut("/{id:int}", async (int id, Rsvp updatedRsvp, EventsDbContext db) =>
        {
            var rsvp = await db.Rsvps.FindAsync(id);
            if (rsvp is null)
                return Results.NotFound();

            if (rsvp.AccessCode != updatedRsvp.AccessCode)
                return Results.Forbid();

            rsvp.Name = updatedRsvp.Name;
            rsvp.Attending = updatedRsvp.Attending;
            rsvp.BringsPlusOne = updatedRsvp.BringsPlusOne;
            rsvp.JoinForDinner = updatedRsvp.JoinForDinner;
            rsvp.Comments = updatedRsvp.Comments;

            await db.SaveChangesAsync();
            return Results.Ok(rsvp);
        });

        group.MapDelete("/{id:int}", async (int id, string accessCode, EventsDbContext db) =>
        {
            var rsvp = await db.Rsvps.FindAsync(id);
            if (rsvp == null)
                return Results.NotFound();

            if (!rsvp.AccessCode.Equals(accessCode, StringComparison.CurrentCultureIgnoreCase))
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
