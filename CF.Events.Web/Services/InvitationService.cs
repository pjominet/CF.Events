using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Services;

public interface IInvitationService
{
    Task<int> ProcessPendingInvitationsAsync(CancellationToken stoppingToken = default);
    Task SendImmediateInvitationsAsync(int eventId, List<string> userIds, string inviteCode, CancellationToken stoppingToken = default);
}

public class InvitationService(
    EventsDbContext db,
    IMailService mailService,
    IOptions<AppSettings> appOptions,
    ILogger<InvitationService> logger) : IInvitationService
{
    private readonly AppSettings _appSettings = appOptions.Value;

    private record InvitationInfo(
        int EventId,
        string UserId,
        string EventName,
        string UserDisplayName,
        string UserEmail,
        string? InvitationInviteCode);

    public async Task<int> ProcessPendingInvitationsAsync(CancellationToken stoppingToken = default)
    {
        var batchSize = _appSettings.EmailBatchSize ?? int.MaxValue;

        var pendingInvitations = await db.UserEvents
            .Where(ue => !ue.InviteEmailSent && ue.ScheduledFor != null && ue.ScheduledFor <= DateTime.UtcNow)
            .OrderBy(ue => ue.ScheduledFor)
            .Take(batchSize)
            .Select(ue => new InvitationInfo(
                ue.EventId,
                ue.UserId,
                ue.Event.Name,
                ue.User.DisplayName!,
                ue.User.Email!,
                ue.Event.InviteCodes.OrderByDescending(c => c.CreatedAt).Select(c => c.Code).FirstOrDefault()))
            .AsSplitQuery()
            .ToListAsync(stoppingToken);

        if (pendingInvitations.Count == 0) return 0;

        logger.LogInformation("Processing {Count} pending invitation emails", pendingInvitations.Count);

        var sentCount = await SendInvitationEmailsAsync(pendingInvitations, stoppingToken);

        return sentCount;
    }

    public async Task SendImmediateInvitationsAsync(int eventId, List<string> userIds, string inviteCode, CancellationToken stoppingToken = default)
    {
        var batchSize = _appSettings.EmailBatchSize ?? int.MaxValue;

        // Fetch user events that haven't been sent yet for this event and these users
        var userEvents = await db.UserEvents
            .Where(ue => ue.EventId == eventId && userIds.Contains(ue.UserId) && !ue.InviteEmailSent)
            .Include(ue => ue.User)
            .Include(ue => ue.Event)
            .AsSplitQuery()
            .ToListAsync(stoppingToken);

        if (userEvents.Count == 0) return;

        List<InvitationInfo> toSend;
        if (userEvents.Count > batchSize)
        {
            logger.LogInformation("Immediate invite count {Count} exceeds batch size {BatchSize}. Sending first batch and scheduling remaining for worker", userEvents.Count, batchSize);

            foreach (var ue in userEvents)
            {
                // Set ScheduledFor to UtcNow for all, so the worker picks up the rest
                ue.ScheduledFor ??= DateTime.UtcNow;
            }

            await db.SaveChangesAsync(stoppingToken);

            // Send the first batch immediately.
            toSend = userEvents
                .Take(batchSize)
                .Select(ue => new InvitationInfo(
                    ue.EventId,
                    ue.UserId,
                    ue.Event.Name,
                    ue.User.DisplayName!,
                    ue.User.Email!,
                    inviteCode))
                .ToList();
        }
        else
        {
            // Send all immediately
            toSend = userEvents
                .Select(ue => new InvitationInfo(
                    ue.EventId,
                    ue.UserId,
                    ue.Event.Name,
                    ue.User.DisplayName!,
                    ue.User.Email!,
                    inviteCode))
                .ToList();
        }

        await SendInvitationEmailsAsync(toSend, stoppingToken);
    }

    private async Task<int> SendInvitationEmailsAsync(List<InvitationInfo> projections, CancellationToken stoppingToken)
    {
        var sentCount = 0;
        var baseUrl = _appSettings.BaseUrl?.TrimEnd('/');

        foreach (var proj in projections)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                var callbackUrl = $"{baseUrl}/events/invite-callback?code={proj.InvitationInviteCode}&email={proj.UserEmail}";

                await mailService.SendInvitationAsync(
                    proj.EventName,
                    proj.UserDisplayName,
                    proj.UserEmail,
                    callbackUrl);

                await db.UserEvents
                    .Where(ue => ue.EventId == proj.EventId && ue.UserId == proj.UserId)
                    .ExecuteUpdateAsync(s => s.SetProperty(ue => ue.InviteEmailSent, true), stoppingToken);

                sentCount++;
                logger.LogInformation("Sent invitation email to {Email} for event {EventName}", proj.UserEmail, proj.EventName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send invitation email to {Email}", proj.UserEmail);
            }
        }

        return sentCount;
    }
}
