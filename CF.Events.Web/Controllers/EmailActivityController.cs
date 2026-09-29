using CF.Events.Web.Models.Requests;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[ApiController]
[Route("api/email-activity")]
[Authorize(Roles = Roles.Admin)]
public class EmailActivityController(
    IEmailActivityService emailActivityService,
    ILogger<EmailActivityController> logger) : ControllerBase
{
    /// <summary>
    /// Fetches email activity from SMTP2GO for the last 24 hours (or specified number of hours),
    /// persists the sent email info, open/click statuses, error messages, and entire event timeline to the database,
    /// and returns the synced activities.
    /// </summary>
    /// <param name="hours">Number of hours to look back (default: 24)</param>
    /// <param name="ctx">Cancellation token</param>
    [HttpPost("fetch-recent")]
    [HttpGet("fetch-recent")]
    public async Task<IActionResult> FetchRecentActivity([FromQuery] int hours = 24, CancellationToken ctx = default)
    {
        try
        {
            if (hours <= 0 || hours > 720) hours = 24;
            var result = await emailActivityService.FetchAndSaveActivityAsync(hours, ctx);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching email activity from SMTP2GO for the last {Hours} hours", hours);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Searches email activity from SMTP2GO based on the provided criteria (date range, sender, recipient, subject, event types),
    /// saves the events and timelines to the database, and returns the result.
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> SearchAndSaveActivity([FromBody] EmailActivityFetchRequest request, CancellationToken ctx = default)
    {
        try
        {
            var result = await emailActivityService.FetchAndSaveActivityAsync(request, ctx);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching email activity from SMTP2GO");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets all saved email activities from the database with their current status and timeline.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActivities([FromQuery] int limit = 100, CancellationToken ctx = default)
    {
        try
        {
            var activities = await emailActivityService.GetRecentActivitiesAsync(limit, ctx);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving email activities");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific email activity and its timeline by SMTP2GO email ID.
    /// </summary>
    [HttpGet("{emailId}")]
    public async Task<IActionResult> GetActivityByEmailId([FromRoute] string emailId, CancellationToken ctx = default)
    {
        try
        {
            var activity = await emailActivityService.GetActivityByEmailIdAsync(emailId, ctx);
            if (activity is null)
                return NotFound(new { error = $"Email activity with ID '{emailId}' was not found." });

            return Ok(activity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving email activity for {EmailId}", emailId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the timeline of events for a specific email by SMTP2GO email ID.
    /// </summary>
    [HttpGet("{emailId}/timeline")]
    public async Task<IActionResult> GetTimelineByEmailId([FromRoute] string emailId, CancellationToken ctx = default)
    {
        try
        {
            var timeline = await emailActivityService.GetTimelineForEmailAsync(emailId, ctx);
            return Ok(timeline);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving timeline for {EmailId}", emailId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
