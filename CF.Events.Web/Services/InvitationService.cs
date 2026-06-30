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
        string? InviteCode);

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

        // Fetch event users that haven't been sent an invitation yet for this event
        var query = db.UserEvents.Where(ue => ue.EventId == eventId && userIds.Contains(ue.UserId) && !ue.InviteEmailSent);
        var eventUsers = await query
            .Select(ue => new InvitationInfo(
                ue.EventId,
                ue.UserId,
                ue.Event.Name,
                ue.User.DisplayName!,
                ue.User.Email!,
                inviteCode))
            .ToListAsync(stoppingToken);

        if (eventUsers.Count == 0) return;

        List<InvitationInfo> toSend;
        if (eventUsers.Count > batchSize)
        {
            logger.LogInformation("Immediate invite count {Count} exceeds batch size {BatchSize}. Sending first batch and scheduling remaining for worker", eventUsers.Count, batchSize);

            // Set ScheduledFor to UtcNow for all event users, so the worker picks up the rest
            await query.ExecuteUpdateAsync(s => s.SetProperty(ue => ue.ScheduledFor, DateTime.UtcNow), stoppingToken);

            // Send the only first batch immediately
            toSend = eventUsers.Take(batchSize).ToList();
        }
        else
        {
            // Send all immediately
            toSend = eventUsers.ToList();
        }

        await SendInvitationEmailsAsync(toSend, stoppingToken);
    }

    private async Task<int> SendInvitationEmailsAsync(List<InvitationInfo> invites, CancellationToken stoppingToken)
    {
        var sentCount = 0;
        var baseUrl = _appSettings.BaseUrl?.TrimEnd('/');

        foreach (var invite in invites)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                var callbackUrl = $"{baseUrl}/events/invite-callback?code={invite.InviteCode}&email={invite.UserEmail}";

                await mailService.SendInvitationAsync(
                    invite.EventName,
                    invite.UserDisplayName,
                    invite.UserEmail,
                    callbackUrl);

                await db.UserEvents
                    .Where(ue => ue.EventId == invite.EventId && ue.UserId == invite.UserId)
                    .ExecuteUpdateAsync(s => s.SetProperty(ue => ue.InviteEmailSent, true), stoppingToken);

                sentCount++;
                logger.LogInformation("Sent invitation email to {Email} for event {EventName}", invite.UserEmail, invite.EventName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send invitation email to {Email}", invite.UserEmail);
            }
        }

        return sentCount;
    }
}
