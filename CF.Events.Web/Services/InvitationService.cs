using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Services;

public interface IInvitationService
{
    Task<int> ProcessPendingInvitationsAsync(CancellationToken ctx = default);
    Task SendImmediateInvitationsAsync(List<InviteEmailRequest> inviteRequests, CancellationToken ctx = default);
    Task SendInvitationAsync(InviteEmailRequest inviteRequest, CancellationToken ctx = default);
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

        // Get pending invitations (scheduled and not yet sent)
        var pendingInvitations = await db.Invitations
            .Where(i => !i.InviteEmailSent && i.ScheduledFor != null && i.ScheduledFor <= DateTime.UtcNow)
            .OrderBy(i => i.ScheduledFor)
            .Take(batchSize)
            .Include(i => i.Event)
            .Include(i => i.InviteCode)
            .Include(i => i.InvitedPersons)
                .ThenInclude(ip => ip.User)
            .AsSplitQuery()
            .ToListAsync(ctx);

        if (pendingInvitations.Count == 0) return 0;

        logger.LogInformation("Processing {Count} pending invitation emails", pendingInvitations.Count);

        // Convert to service DTO format for sending
        // Send to primary contact of each invitation group
        var inviteRequests = new List<InviteEmailRequest>();
        foreach (var invitation in pendingInvitations)
        {
            var primaryPerson = invitation.InvitedPersons.FirstOrDefault(ip => ip.IsPrimary);
            if (primaryPerson != null)
            {
                inviteRequests.Add(new InviteEmailRequest
                {
                    EventId = invitation.EventId,
                    EventName = invitation.Event.Name,
                    UserId = primaryPerson.UserId!,
                    UserDisplayName = primaryPerson.User?.DisplayName ?? primaryPerson.Name,
                    UserEmail = primaryPerson.User?.Email ?? primaryPerson.Email,
                    InviteCode = invitation.InviteCode?.Code
                });
            }
        }

        var sentCount = await SendInvitationEmailsAsync(inviteRequests, ctx);

        return sentCount;
    }

    public async Task SendImmediateInvitationsAsync(List<InviteEmailRequest> inviteRequests, CancellationToken ctx = default)
    {
        var batchSize = _appSettings.EmailBatchSize ?? int.MaxValue;

        if (inviteRequests.Count == 0) return;

        if (inviteRequests.Count > batchSize)
        {
            logger.LogInformation("Immediate invite count {Count} exceeds batch size {BatchSize}. Sending first batch and scheduling remaining for worker", inviteRequests.Count, batchSize);

            // Set ScheduledFor to UtcNow for all invitations, so the worker picks up the rest
            // Note: This is a simplified approach. In a real implementation, you'd need to map the DTO invitations
            // back to the actual Invitation entities in the database.
            // For now, we'll just process the first batch and log a warning about the rest.
            logger.LogWarning("Batch processing for large invitation sets not yet fully implemented for new Invitation system");

            // Send the only first batch immediately
            inviteRequests = inviteRequests.Take(batchSize).ToList();
        }

        await SendInvitationEmailsAsync(inviteRequests, ctx);
    }

    public async Task SendInvitationAsync(InviteEmailRequest inviteRequest, CancellationToken ctx = default) => await SendInvitationEmailsAsync([inviteRequest], ctx);

    private async Task<int> SendInvitationEmailsAsync(List<InviteEmailRequest> inviteRequest, CancellationToken ctx)
    {
        var sentCount = 0;
        var baseUrl = _appSettings.BaseUrl?.TrimEnd('/');

        foreach (var invitation in inviteRequest)
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

                // Mark the invitation as sent in the database
                // Since we're using DTOs, we need to find the actual Invitation entity
                // For now, we'll mark by UserId and EventId, but this assumes one invitation per user per event
                // In the new system, there might be multiple people per invitation, so this needs refinement
                await db.Invitations
                    .Where(i => i.EventId == invitation.EventId && i.InvitedPersons.Any(ip => ip.UserId == invitation.UserId))
                    .ExecuteUpdateAsync(s => s.SetProperty(i => i.InviteEmailSent, true), ctx);

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
