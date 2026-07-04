using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[Route("rsvps")]
[Authorize]
public class RsvpController(
    EventsDbContext db,
    IRsvpService rsvpService,
    ILogger<RsvpController> logger) : ApiController
{
    /// <summary>
    /// Gets the RSVP form data for an invitation.
    /// This is the main endpoint for loading the RSVP stepper form.
    /// </summary>
    [HttpGet("invitation/{invitationId:int}")]
    public async Task<IActionResult> GetRsvpForm(int invitationId)
    {
        var userId = User.GetId();

        // Verify the user is invited to this invitation
        var isInvited = await db.InvitedPersons
            .AnyAsync(ip => ip.InvitationId == invitationId && ip.PrimaryGroupUserId == userId);

        if (!isInvited)
        {
            logger.LogWarning("User {UserId} tried to access RSVP form for invitation {InvitationId} they are not invited to", userId, invitationId);
            return Forbid();
        }

        var formData = await rsvpService.GetRsvpFormAsync(invitationId, userId);

        if (formData is not null) return Ok(formData);

        logger.LogWarning("RSVP form data not found for invitation {InvitationId}", invitationId);
        return NotFound();
    }

    /// <summary>
    /// Gets the current RSVP status for an invitation.
    /// </summary>
    [HttpGet("invitation/{invitationId:int}/status")]
    public async Task<IActionResult> GetRsvpStatus(int invitationId)
    {
        var userId = User.GetId();

        // Verify the user is invited to this invitation
        var isInvited = await db.InvitedPersons
            .AnyAsync(ip => ip.InvitationId == invitationId && ip.PrimaryGroupUserId == userId);

        if (!isInvited)
        {
            return Forbid();
        }

        var rsvp = await rsvpService.GetCurrentRsvpAsync(invitationId);

        if (rsvp is null)
        {
            return Ok(new {
                HasRsvp = false,
                Status = "NotStarted"
            });
        }

        return Ok(new {
            HasRsvp = true,
            rsvp.Status,
            rsvp.SubmittedAt,
            rsvp.CreatedAt,
            rsvp.UpdatedAt
        });
    }

    /// <summary>
    /// Submits the final RSVP (convenience endpoint for final submission).
    /// </summary>
    [HttpPost("invitation/{invitationId:int}/submit")]
    public async Task<IActionResult> SubmitRsvp(int invitationId, [FromBody] RsvpRequest? request)
    {
        if (request is null) return BadRequest(new { success = false, message = "Invalid request body" });

        // Force IsDraft to false for submission
        request.IsDraft = false;
        return await SaveRsvp(invitationId, request);
    }

    /// <summary>
    /// Saves the RSVP as draft (convenience endpoint for saving progress).
    /// </summary>
    [HttpPost("invitation/{invitationId:int}/draft")]
    public async Task<IActionResult> SaveRsvpDraft(int invitationId, [FromBody] RsvpRequest? request)
    {
        if (request is null) return BadRequest(new { success = false, message = "Invalid request body" });

        // Force IsDraft to true for draft save
        request.IsDraft = true;
        return await SaveRsvp(invitationId, request);
    }

    private async Task<IActionResult> SaveRsvp(int invitationId, [FromBody] RsvpRequest? request)
    {
        if (request is null) return BadRequest(new { success = false, message = "Invalid request body" });

        if (request.InvitationId != invitationId)
        {
            return BadRequest(new { success = false, message = "Invitation ID mismatch" });
        }

        var userId = User.GetId();
        var result = await rsvpService.SaveRsvpAsync(request, userId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets a specific RSVP by ID (admin-only).
    /// </summary>
    [HttpGet("{rsvpId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetRsvpById(int rsvpId)
    {
        var rsvp = await db.Rsvps
            .Include(r => r.Event)
            .Include(r => r.Invitation)
                .ThenInclude(i => i.InvitedPersons)
                    .ThenInclude(ip => ip.User)
            .Include(r => r.People)
                .ThenInclude(p => p.FoodPreferences)
            .Include(r => r.People)
                .ThenInclude(p => p.Accommodations)
            .Include(r => r.People)
                .ThenInclude(p => p.InvitedPerson)
            .Include(r => r.CustomAnswers)
                .ThenInclude(ca => ca.Question)
            .FirstOrDefaultAsync(r => r.Id == rsvpId);

        return rsvp is not null ? Ok(rsvp) : NotFound();
    }

    /// <summary>
    /// Gets all RSVPs for an event (admin-only).
    /// </summary>
    [HttpGet("event/{eventId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetRsvpsForEvent(int eventId)
    {
        var rsvps = await db.Rsvps
            .Where(r => r.EventId == eventId)
            .Select(r => new {
                r.Id,
                r.Status,
                r.SubmittedAt,
                r.GroupName,
                r.Comments,
                r.CreatedAt,
                PeopleCount = r.People.Count,
                AttendingCount = r.People.Count(p => p.Attending),
                PrimaryPerson = r.People.OrderByDescending(p => p.IsPrimary).FirstOrDefault(),
                Invitation = new {
                    r.Invitation.Id,
                    r.Invitation.GroupName,
                    InvitedPersons = r.Invitation.InvitedPersons.Select(ip => new {
                        ip.Id,
                        ip.Name,
                        ip.Email,
                        UserId = ip.PrimaryGroupUserId
                    }).ToList()
                }
            })
            .ToListAsync();

        return Ok(rsvps);
    }
}
