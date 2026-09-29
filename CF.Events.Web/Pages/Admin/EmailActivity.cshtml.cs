using CF.Events.Web.Data;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Roles.Admin)]
public class EmailActivityModel(
    EventsDbContext db,
    IEmailActivityService emailActivityService,
    ILogger<EmailActivityModel> logger) : PageModel
{
    public const int DefaultPageSize = 25;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? TimeRange { get; set; } = "7d";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public List<EmailActivity> Activities { get; private set; } = [];

    public int TotalCount { get; private set; }
    public int DeliveredCount { get; private set; }
    public int OpenedCount { get; private set; }
    public int ClickedCount { get; private set; }
    public int FailedCount { get; private set; }

    public int TotalPages { get; private set; }
    public int TotalMatchingItems { get; private set; }

    public async Task OnGetAsync()
    {
        if (PageNumber < 1) PageNumber = 1;

        // Base query with time filter for statistics & listing
        var baseQuery = db.EmailActivities.AsNoTracking();

        var (startDate, _) = GetDateRange(TimeRange);
        if (startDate.HasValue)
            baseQuery = baseQuery.Where(a => a.SentAt >= startDate.Value || a.LatestEventAt >= startDate.Value);

        // Summary KPI counts for the selected time range
        TotalCount = await baseQuery.CountAsync();
        DeliveredCount = await baseQuery.CountAsync(a => a.IsDelivered);
        OpenedCount = await baseQuery.CountAsync(a => a.WasOpened);
        ClickedCount = await baseQuery.CountAsync(a => a.WasClicked);
        FailedCount = await baseQuery.CountAsync(a => a.HasError || a.IsBounced || a.IsSpam);

        // Filtered query for the table
        var filteredQuery = baseQuery;

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var search = Search.Trim().ToLower();
            filteredQuery = filteredQuery.Where(a =>
                a.RecipientEmail.ToLower().Contains(search) ||
                a.FromEmail.ToLower().Contains(search) ||
                (a.Subject != null && a.Subject.ToLower().Contains(search)) ||
                a.EmailId.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(Status) && !Status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            filteredQuery = Status.ToLowerInvariant() switch
            {
                "delivered" => filteredQuery.Where(a => a.IsDelivered),
                "opened" => filteredQuery.Where(a => a.WasOpened),
                "clicked" => filteredQuery.Where(a => a.WasClicked),
                "bounced" => filteredQuery.Where(a => a.IsBounced),
                "failed" => filteredQuery.Where(a => a.HasError || a.IsBounced || a.IsSpam),
                _ => filteredQuery
            };
        }

        TotalMatchingItems = await filteredQuery.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalMatchingItems / (double)DefaultPageSize);
        if (TotalPages == 0) TotalPages = 1;

        if (PageNumber > TotalPages)
            PageNumber = TotalPages;

        Activities = await filteredQuery
            .OrderByDescending(a => a.SentAt)
            .ThenByDescending(a => a.LatestEventAt)
            .Skip((PageNumber - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostSyncNowAsync(int hours = 24)
    {
        try
        {
            if (hours <= 0) hours = 24;
            var result = await emailActivityService.FetchAndSaveActivityAsync(hours);
            return new JsonResult(new
            {
                success = true,
                message = $"Successfully synced email activity! Processed {result.EmailsProcessed} emails ({result.NewEventsAdded} new events, {result.TotalEventsFetched} fetched).",
                data = new
                {
                    result.TotalEventsFetched,
                    result.EmailsProcessed,
                    result.NewEventsAdded,
                    result.UpdatedEmailsCount
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to manually sync email activity");
            return new JsonResult(new
            {
                success = false,
                message = $"Sync failed: {ex.Message}"
            }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnGetTimelineAsync(string emailId)
    {
        if (string.IsNullOrWhiteSpace(emailId))
            return BadRequest(new { success = false, message = "Email ID is required." });

        var activity = await db.EmailActivities
            .AsNoTracking()
            .Include(a => a.Timeline.OrderBy(t => t.EventAt))
            .FirstOrDefaultAsync(a => a.EmailId == emailId);

        if (activity is null)
            return NotFound(new { success = false, message = "Email activity not found." });

        return new JsonResult(new
        {
            success = true,
            emailId = activity.EmailId,
            from = activity.FromEmail,
            recipient = activity.RecipientEmail,
            subject = activity.Subject,
            sentAt = activity.SentAt.ToString("yyyy-MM-dd HH:mm:ss"),
            latestEvent = activity.LatestEvent,
            latestEventAt = activity.LatestEventAt.ToString("yyyy-MM-dd HH:mm:ss"),
            isDelivered = activity.IsDelivered,
            isOpened = activity.WasOpened,
            openCount = activity.OpenCount,
            firstOpenedAt = activity.FirstOpenedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
            isClicked = activity.WasClicked,
            clickCount = activity.ClickCount,
            firstClickedAt = activity.FirstClickedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
            hasError = activity.HasError,
            errorMessage = activity.LastErrorMessage,
            smtpResponse = activity.LastSmtpResponse,
            events = activity.Timeline.Select(e => new
            {
                emailActivityId = e.EmailActivityId,
                emailId = e.EmailId,
                @event = e.Event,
                eventAt = e.EventAt.ToString("yyyy-MM-dd HH:mm:ss"),
                smtpResponse = e.SmtpResponse,
                errorMessage = e.ErrorMessage,
                host = e.Host,
                userAgent = e.UserAgent,
                clickUrl = e.ClickUrl
            }).ToList()
        });
    }

    private static (DateTime? StartDate, DateTime? EndDate) GetDateRange(string? range)
    {
        var now = DateTime.UtcNow;
        return range?.ToLowerInvariant() switch
        {
            "24h" => (now.AddHours(-24), now),
            "7d" => (now.AddDays(-7), now),
            "30d" => (now.AddDays(-30), now),
            "all" => (null, null),
            _ => (now.AddDays(-7), now)
        };
    }
}
