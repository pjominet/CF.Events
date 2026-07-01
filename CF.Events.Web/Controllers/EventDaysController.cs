using CF.Events.Web.Data;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[Route("events/{eventId:int}/days")]
[Authorize(Roles = Roles.Admin)]
public class EventDaysController(
    EventsDbContext db,
    ILogger<EventDaysController> logger) : Controller
{
    /// <summary>
    /// Gets all days for an event.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(int eventId)
    {
        var days = await db.EventDays
            .Where(d => d.EventId == eventId)
            .OrderBy(d => d.Date)
            .Select(d => new
            {
                d.Id,
                d.EventId,
                d.Date,
                d.Name,
                d.OffersFood,
                d.OffersAccommodation
            })
            .ToListAsync();

        return Ok(days);
    }

    /// <summary>
    /// Gets a single event day by ID.
    /// </summary>
    [HttpGet("{dayId:int}")]
    public async Task<IActionResult> Get(int eventId, int dayId)
    {
        var day = await db.EventDays
            .Where(d => d.EventId == eventId && d.Id == dayId)
            .Select(d => new
            {
                d.Id,
                d.EventId,
                d.Date,
                d.Name,
                d.OffersFood,
                d.OffersAccommodation
            })
            .FirstOrDefaultAsync();

        if (day is null)
            return NotFound();

        return Ok(day);
    }

    /// <summary>
    /// Creates a new event day.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(int eventId, [FromBody] EventDayRequest request)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev is null)
            return NotFound("Event not found");

        if (request.Date < ev.Date.Date || request.Date > ev.EndDate.Date)
        {
            return BadRequest($"Date must be between {ev.Date:yyyy-MM-dd} and {ev.EndDate:yyyy-MM-dd}");
        }

        var exists = await db.EventDays
            .AnyAsync(d => d.EventId == eventId && d.Date == request.Date);

        if (exists)
            return BadRequest("An event day already exists for this date");

        var day = new EventDay
        {
            EventId = eventId,
            Date = request.Date,
            Name = request.Name,
            OffersFood = request.OffersFood,
            OffersAccommodation = request.OffersAccommodation
        };

        db.EventDays.Add(day);
        await db.SaveChangesAsync();

        logger.LogInformation("Created event day {DayId} for event {EventId}", day.Id, eventId);

        return CreatedAtAction(nameof(Get), new { eventId, dayId = day.Id }, new
        {
            day.Id,
            day.EventId,
            day.Date,
            day.Name,
            day.OffersFood,
            day.OffersAccommodation
        });
    }

    /// <summary>
    /// Updates an existing event day.
    /// </summary>
    [HttpPut("{dayId:int}")]
    public async Task<IActionResult> Update(int eventId, int dayId, [FromBody] EventDayRequest request)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev is null)
            return NotFound("Event not found");

        var day = await db.EventDays
            .FirstOrDefaultAsync(d => d.EventId == eventId && d.Id == dayId);

        if (day is null)
            return NotFound();

        if (request.Date < ev.Date.Date || request.Date > ev.EndDate.Date)
        {
            return BadRequest($"Date must be between {ev.Date:yyyy-MM-dd} and {ev.EndDate:yyyy-MM-dd}");
        }

        // Check for duplicate date (excluding current day)
        var duplicateDate = await db.EventDays
            .AnyAsync(d => d.EventId == eventId && d.Date == request.Date && d.Id != dayId);

        if (duplicateDate)
            return BadRequest("Another event day already exists for this date");

        day.Date = request.Date;
        day.Name = request.Name;
        day.OffersFood = request.OffersFood;
        day.OffersAccommodation = request.OffersAccommodation;

        await db.SaveChangesAsync();

        logger.LogInformation("Updated event day {DayId} for event {EventId}", dayId, eventId);

        return Ok(new
        {
            day.Id,
            day.EventId,
            day.Date,
            day.Name,
            day.OffersFood,
            day.OffersAccommodation
        });
    }

    /// <summary>
    /// Deletes an event day and its related food preferences and accommodations.
    /// </summary>
    [HttpDelete("{dayId:int}")]
    public async Task<IActionResult> Delete(int eventId, int dayId)
    {
        var day = await db.EventDays
            .FirstOrDefaultAsync(d => d.EventId == eventId && d.Id == dayId);

        if (day is null)
            return NotFound();

        // Cascade delete will handle related RsvpFoodPreference and RsvpAccommodation records
        db.EventDays.Remove(day);
        await db.SaveChangesAsync();

        logger.LogInformation("Deleted event day {DayId} for event {EventId}", dayId, eventId);

        return NoContent();
    }

    /// <summary>
    /// Auto-generates event days for all dates between event start and end date.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateDays(int eventId)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev is null)
            return NotFound("Event not found");

        var existingDates = await db.EventDays
            .Where(d => d.EventId == eventId)
            .Select(d => d.Date)
            .ToListAsync();

        var newDays = new List<EventDay>();
        var dayNumber = existingDates.Count + 1;

        for (var date = ev.Date.Date; date <= ev.EndDate.Date; date = date.AddDays(1))
        {
            if (existingDates.Contains(date))
                continue;

            newDays.Add(new EventDay
            {
                EventId = eventId,
                Date = date,
                Name = $"Day {dayNumber}",
                OffersFood = true,
                OffersAccommodation = true
            });
            dayNumber++;
        }

        if (newDays.Count == 0)
            return Ok(new { Message = "All dates already have event days", Created = 0 });

        db.EventDays.AddRange(newDays);
        await db.SaveChangesAsync();

        logger.LogInformation("Generated {Count} event days for event {EventId}", newDays.Count, eventId);

        return Ok(new
        {
            Message = $"Created {newDays.Count} event day(s)",
            Created = newDays.Count,
            Days = newDays.Select(d => new
            {
                d.Id,
                d.EventId,
                d.Date,
                d.Name,
                d.OffersFood,
                d.OffersAccommodation
            })
        });
    }
}

public class EventDayRequest
{
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool OffersFood { get; set; } = true;
    public bool OffersAccommodation { get; set; } = true;
}
