using CF.Events.Web.Data;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[Route("admin/email/activity")]
[Authorize(Roles = Roles.Admin)]
public class EmailActivityController(
    EventsDbContext db,
    IEmailActivityService emailActivityService,
    ILogger<EmailActivityController> logger) : Controller
{
    [HttpPost("sync")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncNow([FromQuery] int hours = 24)
    {
        try
        {
            if (hours <= 0) hours = 24;
            var result = await emailActivityService.FetchAndSaveActivityAsync(hours);

            var message = $"Successfully synced email activity! Processed {result.EmailsProcessed} emails ({result.NewEventsAdded} new events, {result.TotalEventsFetched} fetched).";

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to manually sync email activity");
            return StatusCode(500, new { success = false, message = $"Sync failed: {ex.Message}" });
        }
    }

    [HttpGet("timeline/{emailId}")]
    public async Task<IActionResult> GetTimeline([FromRoute] string emailId)
    {
        if (string.IsNullOrEmpty(emailId))
            return BadRequest("Email ID is required.");

        var activity = await db.EmailActivities
            .AsNoTracking()
            .Include(a => a.TimelineEvents)
            .FirstOrDefaultAsync(a => a.EmailId == emailId);

        if (activity is null)
            return NotFound("Email activity not found.");

        return PartialView("~/Pages/Admin/Shared/_EmailTimeline.cshtml", activity);
    }
}
