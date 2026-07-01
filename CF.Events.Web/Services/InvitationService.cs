using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
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

    /// <summary>
    /// Creates an InviteEmailRequest from an invitation and its primary invited person.
    /// Generates a per-user, single-use invitation token and stores it on the InvitedPerson.
    /// </summary>
    InviteEmailRequest CreateInviteEmailRequest(Invitation invitation, InvitedPerson primaryPerson, int tokenValidDays = 90);
}

public class InvitationService(
    EventsDbContext db,
    IMailService mailService,
    IOptions<AppSettings> appOptions,
    ILogger<InvitationService> logger) : IInvitationService
{
    private readonly AppSettings _appSettings = appOptions.Value;

    /// <summary>
    /// Creates an InviteEmailRequest from an invitation and its primary invited person.
    /// Generates a per-user, single-use invitation token and stores it on the InvitedPerson.
    /// </summary>
    public InviteEmailRequest CreateInviteEmailRequest(Invitation invitation, InvitedPerson primaryPerson, int tokenValidDays = 90)
    {
        var token = CodeGenerator.Generate(64);
        primaryPerson.InvitationToken = token;
        primaryPerson.InvitationTokenExpiresAt = DateTime.UtcNow.AddDays(tokenValidDays);

        return new InviteEmailRequest
        {
            EventId = invitation.EventId,
            EventName = invitation.Event.Name,
            UserId = primaryPerson.UserId!,
            UserDisplayName = primaryPerson.User?.DisplayName ?? primaryPerson.Name,
            UserEmail = primaryPerson.User?.Email ?? primaryPerson.Email!,
            InvitationToken = token
        };
    }

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
            if (primaryPerson != null && invitation.InviteCode != null)
            {
                inviteRequests.Add(CreateInviteEmailRequest(invitation, primaryPerson));
            }
        }

        // Save the generated invitation tokens to the database
        await db.SaveChangesAsync(ctx);

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

        foreach (var request in inviteRequest)
        {
            if (ctx.IsCancellationRequested) break;

            try
            {
                var callbackUrl = $"{baseUrl}/events/invite-callback?token={request.InvitationToken}";

                await mailService.SendInvitationAsync(
                    request.EventName,
                    request.UserDisplayName,
                    request.UserEmail,
                    callbackUrl,
                    ctx: ctx);

                // Mark the invitation as sent in the database
                // Find the specific invitation by UserId and EventId, then mark it as sent
                // Note: This updates all invitations for this user+event, which works for now
                // since typically a user has one invitation per event in the new system
                await db.Invitations
                    .Where(i => i.EventId == request.EventId && i.InvitedPersons.Any(ip => ip.UserId == request.UserId))
                    .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.InviteEmailSent, true), ctx);

                sentCount++;
                logger.LogInformation("Sent invitation email to {Email} for event {EventName}", request.UserEmail, request.EventName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send invitation email to {Email}", request.UserEmail);
            }
        }

        return sentCount;
    }
}
