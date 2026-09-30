using System.Globalization;
using System.Text.Json;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using CF.Smtp2Go.Net;
using CF.Smtp2Go.Net.Models.Requests;
using CF.Smtp2Go.Net.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Services;

public interface IEmailActivityService
{
    Task<EmailActivitySyncResult> FetchAndSaveActivityAsync(int hours = 24, CancellationToken ctx = default);
}

public class EmailActivityService(
    ISmtp2GoClient smtp2GoClient,
    EventsDbContext db,
    ILogger<EmailActivityService> logger) : IEmailActivityService
{
    private static readonly HashSet<string> ErrorEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "soft-bounced",
        "hard-bounced",
        "rejected",
        "spam"
    };

    public Task<EmailActivitySyncResult> FetchAndSaveActivityAsync(int hours = 24, CancellationToken ctx = default)
    {
        if (hours <= 0) hours = 24;
        var now = DateTime.UtcNow;
        var startDate = now.AddHours(-hours);
        return FetchAndSaveActivityAsync(new EmailActivityFetchRequest
        {
            StartDate = startDate,
            EndDate = now,
            Hours = hours
        }, ctx);
    }

    private async Task<EmailActivitySyncResult> FetchAndSaveActivityAsync(EmailActivityFetchRequest request, CancellationToken ctx = default)
    {
        var startDate = request.StartDate ?? (request.Hours.HasValue ? DateTime.UtcNow.AddHours(-request.Hours.Value) : DateTime.UtcNow.AddHours(-24));
        var endDate = request.EndDate ?? DateTime.UtcNow;

        var allEvents = new List<Smtp2GoActivityEventItem>();
        string? continueToken = null;
        var page = 0;
        const int maxPages = 20;

        do
        {
            var searchRequest = new Smtp2GoActivitySearchRequest
            {
                StartDate = startDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                EndDate = endDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                Search = request.Search,
                SearchSender = request.SearchSender,
                SearchRecipient = request.SearchRecipient,
                SearchSubject = request.SearchSubject,
                EventTypes = request.EventTypes,
                Limit = 1000,
                ContinueToken = continueToken
            };

            var apiResponse = await smtp2GoClient.SearchActivityAsync(searchRequest, ctx);
            var searchData = DeserializeActivityData(apiResponse.Data);

            if (searchData?.Events is { Count: > 0 })
                allEvents.AddRange(searchData.Events);

            continueToken = searchData?.ContinueToken;
            page++;
        } while (continueToken.HasValue() && page < maxPages);

        logger.LogInformation("Fetched {Count} email activity events from SMTP2GO", allEvents.Count);

        var validEvents = allEvents
            .Where(e => e.EmailId.HasValue())
            .ToList();

        var groupedEvents = validEvents
            .GroupBy(e => e.EmailId!)
            .ToList();

        var emailIds = groupedEvents.Select(g => g.Key).ToList();

        var existingActivities = await db.EmailActivities
            .Include(a => a.TimelineEvents)
            .Where(a => emailIds.Contains(a.EmailId))
            .ToDictionaryAsync(a => a.EmailId, ctx);

        var newEventsCount = 0;
        var updatedEmailsCount = 0;

        foreach (var group in groupedEvents)
        {
            var emailId = group.Key;
            var isNewActivity = false;

            if (!existingActivities.TryGetValue(emailId, out var activity))
            {
                var firstItem = group.First();
                activity = new EmailActivity
                {
                    EmailId = emailId,
                    FromEmail = firstItem.EffectiveSender,
                    RecipientEmail = firstItem.EffectiveRecipient,
                    Subject = firstItem.Subject,
                    SentAt = ParseEventDate(firstItem.Date),
                    TimelineEvents = []
                };
                db.EmailActivities.Add(activity);
                existingActivities[emailId] = activity;
                isNewActivity = true;
            }
            else
            {
                var sender = group.FirstOrDefault(e => e.EffectiveSender.HasValue())?.EffectiveSender;
                if (sender.HasValue() && !activity.FromEmail.HasValue())
                    activity.FromEmail = sender;

                var recipient = group.FirstOrDefault(e => e.EffectiveRecipient.HasValue())?.EffectiveRecipient;
                if (recipient.HasValue() && !activity.RecipientEmail.HasValue())
                    activity.RecipientEmail = recipient;

                var subject = group.FirstOrDefault(e => e.Subject.HasValue())?.Subject;
                if (subject.HasValue() && !activity.Subject.HasValue())
                    activity.Subject = subject;
            }

            var activityTimelineUpdated = false;

            // Sort group events by date ascending
            var orderedTimelineEvents = group
                .OrderBy(e => ParseEventDate(e.Date))
                .ToList();

            foreach (var item in orderedTimelineEvents)
            {
                var eventDate = ParseEventDate(item.Date);
                var eventType = item.Event ?? "unknown";
                var clickUrl = item.EffectiveUrl;

                var existingTimelineEvent = activity.TimelineEvents.FirstOrDefault(t =>
                    t.Event.Equals(eventType, StringComparison.OrdinalIgnoreCase) &&
                    t.EventAt == eventDate &&
                    (!clickUrl.HasValue() || t.ClickUrl == clickUrl));

                if (existingTimelineEvent is not null) continue;

                var isError = ErrorEvents.Contains(eventType);

                var timelineEvent = new EmailActivityEvent
                {
                    EmailId = emailId,
                    Event = eventType,
                    EventAt = eventDate,
                    SmtpResponse = item.SmtpResponse,
                    ErrorMessage = isError ? item.SmtpResponse : null,
                    Host = item.Host,
                    UserAgent = item.UserAgent,
                    ClickUrl = clickUrl,
                    EmailActivity = activity
                };

                activity.TimelineEvents.Add(timelineEvent);
                newEventsCount++;
                activityTimelineUpdated = true;
            }

            if (!activityTimelineUpdated && !isNewActivity) continue;

            UpdateAggregateStatus(activity);
            updatedEmailsCount++;
        }

        await db.SaveChangesAsync(ctx);

        return new EmailActivitySyncResult(
            TotalEventsFetched: allEvents.Count,
            EmailsProcessed: groupedEvents.Count,
            NewEventsAdded: newEventsCount,
            UpdatedEmailsCount: updatedEmailsCount
        );
    }

    private static void UpdateAggregateStatus(EmailActivity activity)
    {
        if (activity.TimelineEvents.Count == 0) return;

        var orderedTimeline = activity.TimelineEvents.OrderBy(t => t.EventAt).ToList();
        var earliest = orderedTimeline.First();

        activity.SentAt = earliest.EventAt;

        // Delivery status
        activity.IsDelivered = activity.TimelineEvents.Any(t =>
            t.Event.Equals("delivered", StringComparison.OrdinalIgnoreCase) ||
            t.Event.Equals("opened", StringComparison.OrdinalIgnoreCase) ||
            t.Event.Equals("clicked", StringComparison.OrdinalIgnoreCase));

        // Open tracking
        var openEvents = activity.TimelineEvents
            .Where(t => t.Event.Equals("opened", StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.EventAt)
            .ToList();

        activity.WasOpened = openEvents.Count > 0 || activity.TimelineEvents.Any(t => t.Event.Equals("clicked", StringComparison.OrdinalIgnoreCase));
        activity.OpenCount = openEvents.Count;
        activity.FirstOpenedAt = openEvents.FirstOrDefault()?.EventAt;
        activity.LastOpenedAt = openEvents.LastOrDefault()?.EventAt;

        // Click tracking
        var clickEvents = activity.TimelineEvents
            .Where(t => t.Event.Equals("clicked", StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.EventAt)
            .ToList();

        activity.WasClicked = clickEvents.Count > 0;
        activity.ClickCount = clickEvents.Count;
        activity.FirstClickedAt = clickEvents.FirstOrDefault()?.EventAt;
        activity.LastClickedAt = clickEvents.LastOrDefault()?.EventAt;

        // Bounce / Spam status
        activity.IsBounced = activity.TimelineEvents.Any(t =>
            t.Event.Equals("soft-bounced", StringComparison.OrdinalIgnoreCase) ||
            t.Event.Equals("hard-bounced", StringComparison.OrdinalIgnoreCase));

        activity.IsSpam = activity.TimelineEvents.Any(t =>
            t.Event.Equals("spam", StringComparison.OrdinalIgnoreCase));

        // Sandboxed status (separated from email provider error values)
        activity.IsSandboxed = activity.TimelineEvents.Any(t =>
            t.ErrorMessage.HasValue() && t.ErrorMessage.Equals("sandboxed", StringComparison.OrdinalIgnoreCase));

        // Error status (exclude Sandboxed from error state)
        var errorEvent = orderedTimeline.LastOrDefault(t =>
            ErrorEvents.Contains(t.Event) || (t.ErrorMessage.HasValue() && !t.ErrorMessage.Contains("sandboxed", StringComparison.OrdinalIgnoreCase)));

        activity.HasError = errorEvent is not null;
        activity.LastErrorMessage = errorEvent?.ErrorMessage;
        activity.LastSmtpResponse = orderedTimeline.LastOrDefault(t => t.SmtpResponse.HasValue())?.SmtpResponse;

        activity.UpdatedAt = DateTime.UtcNow;
    }

    private static Smtp2GoActivitySearchData? DeserializeActivityData(JsonElement data)
    {
        if (data.ValueKind is not JsonValueKind.Object)
            return null;

        return JsonSerializer.Deserialize<Smtp2GoActivitySearchData>(data.GetRawText(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private static DateTime ParseEventDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return DateTime.UtcNow;

        return DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : DateTime.UtcNow;
    }
}
