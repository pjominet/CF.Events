using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Services;

public interface IInvitationService
{
    Task<int> ProcessPendingInvitationsAsync(CancellationToken ctx = default);
    Task SendImmediateInvitationsAsync(List<InviteEmailRequest> invitations, CancellationToken ctx = default);
    Task SendInvitationAsync(InviteEmailRequest inviteEmailRequest, CancellationToken ctx = default);
}

public class InvitationService(
    EventsDbContext db,
    IMailService mailService,
    IOptions<AppSettings> appOptions,
    ILogger<InvitationService> logger) : IInvitationService
{
    private readonly AppSettings _appSettings = appOptions.Value;

    public async Task<int> ProcessPendingInvitationsAsync(CancellationToken ctx = default)
    {
        var batchSize = _appSettings.EmailBatchSize ?? int.MaxValue;

        var pendingInvitations = await db.EventUsers
            .Where(ue => !ue.InviteEmailSent && ue.ScheduledFor != null && ue.ScheduledFor <= DateTime.UtcNow)
            .OrderBy(ue => ue.ScheduledFor)
            .Take(batchSize)
            .Select(ue => new InviteEmailRequest
            {
                EventId = ue.EventId,
                UserId = ue.UserId,
                EventName = ue.Event.Name,
                UserDisplayName = ue.User.DisplayName!,
                UserEmail = ue.User.Email!,
                InviteCode = ue.Event.InviteCodes
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => c.Code)
                    .FirstOrDefault()
            })
            .AsSplitQuery()
            .ToListAsync(ctx);

        if (pendingInvitations.Count == 0) return 0;

        logger.LogInformation("Processing {Count} pending invitation emails", pendingInvitations.Count);

        var sentCount = await SendInvitationEmailsAsync(pendingInvitations, ctx);

        return sentCount;
    }

    public async Task SendImmediateInvitationsAsync(List<InviteEmailRequest> invitations, CancellationToken ctx = default)
    {
        var batchSize = _appSettings.EmailBatchSize ?? int.MaxValue;

        if (invitations.Count == 0) return;

        if (invitations.Count > batchSize)
        {
            logger.LogInformation("Immediate invite count {Count} exceeds batch size {BatchSize}. Sending first batch and scheduling remaining for worker", invitations.Count, batchSize);

            // Set ScheduledFor to UtcNow for all event users, so the worker picks up the rest
            var eventUsers = invitations
                .Select(i => new { i.EventId, i.UserId })
                .ToList();
            await db.EventUsers
                .Where(ue => !ue.InviteEmailSent && eventUsers.Contains(new { ue.EventId, ue.UserId }))
                .ExecuteUpdateAsync(s => s.SetProperty(ue => ue.ScheduledFor, DateTime.UtcNow), ctx);

            // Send the only first batch immediately
            invitations = invitations.Take(batchSize).ToList();
        }

        await SendInvitationEmailsAsync(invitations, ctx);
    }

    public async Task SendInvitationAsync(InviteEmailRequest inviteEmailRequest, CancellationToken ctx = default)
        => await SendInvitationEmailsAsync([inviteEmailRequest], ctx);

    private async Task<int> SendInvitationEmailsAsync(List<InviteEmailRequest> invitations, CancellationToken ctx)
    {
        var sentCount = 0;
        var baseUrl = _appSettings.BaseUrl?.TrimEnd('/');

        foreach (var invitation in invitations)
        {
            if (ctx.IsCancellationRequested) break;

            try
            {
                var callbackUrl = $"{baseUrl}/events/invite-callback?code={invitation.InviteCode}&email={invitation.UserEmail}";

                await mailService.SendInvitationAsync(
                    invitation.EventName,
                    invitation.UserDisplayName,
                    invitation.UserEmail,
                    callbackUrl,
                    ctx: ctx);

                await db.EventUsers
                    .Where(ue => ue.EventId == invitation.EventId && ue.UserId == invitation.UserId)
                    .ExecuteUpdateAsync(s => s.SetProperty(ue => ue.InviteEmailSent, true), ctx);

                sentCount++;
                logger.LogInformation("Sent invitation email to {Email} for event {EventName}", invitation.UserEmail, invitation.EventName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send invitation email to {Email}", invitation.UserEmail);
            }
        }

        return sentCount;
    }
}
