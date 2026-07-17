using System.Linq.Expressions;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Services;

public interface IInvitationService
{
    AppSettings AppSettings { get; }
    Task<int> ProcessPendingEmails(CancellationToken ctx = default);
    Task SendBatchedEmails<T>(List<T> requests, CancellationToken ctx = default) where T : class, IEmailRequest;
    Task SendEmail<T>(T request, CancellationToken ctx = default) where T : class, IEmailRequest;
    Task<int> InviteUsersAsync(int eventId, UsersInviteRequest inviteRequest, CancellationToken ctx = default);
    Task ResendInvitesAsync(int eventId, List<string> userIds, CancellationToken ctx = default);
}

public class InvitationService(
    EventsDbContext db,
    IMailService mailService,
    IOptions<AppSettings> appOptions,
    ILogger<InvitationService> logger) : IInvitationService
{
    public AppSettings AppSettings { get; } = appOptions.Value;
    private readonly AppSettings _appSettings = appOptions.Value;

    public async Task<int> ProcessPendingEmails(CancellationToken ctx = default)
    {
        var sentInvitations = await ProcessPendingType<InvitationEmailRequest>(
            ue => !ue.InviteEmailSent && ue.ScheduledFor != null && ue.ScheduledFor <= DateTime.UtcNow,
            ue => new InvitationEmailRequest
            {
                EventId = ue.EventId,
                UserId = ue.UserId,
                EventName = ue.Event.Name,
                UserName = ue.User.DisplayName!,
                UserEmail = ue.User.Email!,
                TemplateId = ue.Event.InvitationTemplateId ?? string.Empty,
                CallbackValidity = ue.Event.InviteValidity
            }, ctx);

        var sentSaveTheDates = await ProcessPendingType<SaveDateEmailRequest>(
            ue => !ue.SaveTheDateEmailSent && ue.ScheduledFor != null && ue.ScheduledFor <= DateTime.UtcNow,
            ue => new SaveDateEmailRequest
            {
                EventId = ue.EventId,
                UserId = ue.UserId,
                EventName = ue.Event.Name,
                UserName = ue.User.DisplayName!,
                UserEmail = ue.User.Email!,
                TemplateId = ue.Event.SaveDateTemplateId ?? string.Empty
            }, ctx);

        return sentInvitations + sentSaveTheDates;
    }

    private async Task<int> ProcessPendingType<T>(
        Expression<Func<EventUser, bool>> predicate,
        Expression<Func<EventUser, T>> selector,
        CancellationToken ctx) where T : class, IEmailRequest
    {
        var batchSize = _appSettings.EmailBatchSize ?? int.MaxValue;

        var pending = await db.EventUsers
            .Where(predicate)
            .OrderBy(ue => ue.ScheduledFor)
            .Take(batchSize)
            .Select(selector)
            .ToListAsync(ctx);

        if (pending.Count == 0) return 0;

        foreach (var request in pending)
        {
            await PrepareInvitationAsync(request, ctx);
        }

        await db.SaveChangesAsync(ctx);

        logger.LogInformation("Processing {Count} pending {Type} emails", pending.Count, typeof(T).Name);

        await SendBatchedEmails(pending, ctx);

        return pending.Count;
    }

    public async Task SendBatchedEmails<T>(List<T> requests, CancellationToken ctx = default) where T : class, IEmailRequest
    {
        // filter out requests with non-sendable email addresses
        requests = requests.Where(r => IsSendableEmail(r.UserEmail)).ToList();

        if (requests.Count == 0) return;

        var batchSize = _appSettings.EmailBatchSize ?? int.MaxValue;
        var toSendImmediately = requests;

        if (requests.Count > batchSize)
        {
            logger.LogInformation("Immediate {Type} count {Count} exceeds batch size {BatchSize}. Sending first batch and scheduling remaining for worker",
                typeof(T).Name, requests.Count, batchSize);

            var remainingRequests = requests.Skip(batchSize).Select(r => new { r.EventId, r.UserId }).ToList();

            foreach (var request in remainingRequests)
            {
                await db.EventUsers
                    .Where(ue => ue.EventId == request.EventId && ue.UserId == request.UserId)
                    .ExecuteUpdateAsync(s => s.SetProperty(ue => ue.ScheduledFor, DateTime.UtcNow), ctx);
            }

            toSendImmediately = requests.Take(batchSize).ToList();
        }

        foreach (var request in toSendImmediately)
        {
            if (ctx.IsCancellationRequested) break;
            await SendEmail(request, ctx);
        }
    }

    public async Task SendEmail<T>(T request, CancellationToken ctx = default) where T : class, IEmailRequest
    {
        try
        {
            switch (request)
            {
                case InvitationEmailRequest inv when string.IsNullOrEmpty(inv.TemplateId):
                    logger.LogWarning("No invitation template ID found for event {EventId}", inv.EventId);
                    return;
                case InvitationEmailRequest inv:
                    await mailService.SendInvitationAsync(inv, ctx);
                    await db.EventUsers
                        .Where(ue => ue.EventId == inv.EventId && ue.UserId == inv.UserId)
                        .ExecuteUpdateAsync(s => s.SetProperty(ue => ue.InviteEmailSent, true), ctx);
                    break;
                case SaveDateEmailRequest std when string.IsNullOrEmpty(std.TemplateId):
                    logger.LogWarning("No save the date template ID found for event {EventId}", std.EventId);
                    return;
                case SaveDateEmailRequest std:
                    await mailService.SendSaveTheDateAsync(std, ctx);
                    await db.EventUsers
                        .Where(ue => ue.EventId == std.EventId && ue.UserId == std.UserId)
                        .ExecuteUpdateAsync(s => s.SetProperty(ue => ue.SaveTheDateEmailSent, true), ctx);
                    break;
            }

            logger.LogInformation("Sent {Type} email to {Email} for event {EventName}", typeof(T).Name, request.UserEmail, request.EventName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send {Type} email to {Email}", typeof(T).Name, request.UserEmail);
        }
    }

    public async Task<int> InviteUsersAsync(int eventId, UsersInviteRequest inviteRequest, CancellationToken ctx = default)
    {
        // Validate event exists and is active
        var eventExists = await db.Events.AnyAsync(e => e.Id == eventId && e.IsActive, ctx);
        if (!eventExists)
            throw new ArgumentException("Event not found or not active anymore");

        // Get users who are already invited to this event
        var existingUserIds = await db.EventUsers
            .Where(ue => ue.EventId == eventId)
            .Select(ue => ue.UserId)
            .ToListAsync(ctx);

        // Filter to only new users (not already invited)
        var newUserIds = inviteRequest.UserIds
            .Where(userId => !existingUserIds.Contains(userId))
            .ToList();

        if (newUserIds.Count == 0)
            return 0;

        if (inviteRequest.AllowAccommodationCode)
        {
            var isValidCode = await db.Events
                .Where(e => e.Id == eventId)
                .AnyAsync(e => e.AccommodationCodes.Any(c => c == inviteRequest.SelectedAccommodationCode), ctx);

            if (!isValidCode)
                throw new ArgumentException("Selected accommodation code does not exist or is invalid");
        }

        var newEventUsers = newUserIds.Select(userId => new EventUser
        {
            EventId = eventId,
            UserId = userId,
            AssignedAccommodationCode = inviteRequest.AllowAccommodationCode ? inviteRequest.SelectedAccommodationCode : null,
            ScheduledFor = inviteRequest.ScheduledFor,
            InviteEmailSent = false
        }).ToList();

        db.EventUsers.AddRange(newEventUsers);

        var count = await db.SaveChangesAsync(ctx);

        // Only send emails for immediate invites (not scheduled ones)
        if (inviteRequest is not { SendEmailsOnInvite: SendEmailAction.Immediately, ScheduledFor: null })
            return count;

        var newInvitations = await db.EventUsers
            .Where(ue => ue.EventId == eventId && newUserIds.Contains(ue.UserId))
            .Select(ue => new InvitationEmailRequest
            {
                TemplateId = ue.Event.InvitationTemplateId ?? string.Empty,
                EventId = ue.EventId,
                UserId = ue.UserId,
                EventName = ue.Event.Name,
                UserName = ue.User.DisplayName!,
                UserEmail = ue.User.Email!,
                CallbackValidity = ue.Event.InviteValidity
            })
            .ToListAsync(ctx);

        foreach (var newInvitation in newInvitations)
        {
            await PrepareInvitationAsync(newInvitation, ctx);
        }
        await db.SaveChangesAsync(ctx);

        await SendBatchedEmails(newInvitations, ctx);

        return count;
    }

    public async Task ResendInvitesAsync(int eventId, List<string> userIds, CancellationToken ctx = default)
    {
        var eventUsers = await db.EventUsers
            .Include(eu => eu.User)
            .Where(eu => eu.EventId == eventId && userIds.Contains(eu.UserId))
            .Select(eu => new { eu.UserId, eu.User.Email, eu.User.DisplayName })
            .ToListAsync(ctx);

        if (eventUsers.Count == 0)
            throw new ArgumentException("No users found to resend invitations");

        var eventData = await db.Events
            .Where(e => e.Id == eventId)
            .Select(e => new { e.Id, e.Name, e.InvitationTemplateId, e.InviteValidity })
            .FirstOrDefaultAsync(ctx);

        if (eventData is null)
            throw new ArgumentException("Event does not exist");

        var requests = new List<InvitationEmailRequest>();

        foreach (var user in eventUsers)
        {
            var request = new InvitationEmailRequest
            {
                TemplateId = eventData.InvitationTemplateId ?? string.Empty,
                EventId = eventData.Id,
                EventName = eventData.Name,
                UserName = user.DisplayName!,
                UserEmail = user.Email!,
                UserId = user.UserId,
                CallbackValidity = eventData.InviteValidity
            };

            await PrepareInvitationAsync(request, ctx);
            requests.Add(request);
        }

        await db.SaveChangesAsync(ctx);

        if (requests.Count > 0)
            await SendBatchedEmails(requests, ctx);
    }

    private async Task PrepareInvitationAsync(IEmailRequest request, CancellationToken ctx = default)
    {
        var code = CodeGenerator.Generate(64);
        await db.InviteCodes.AddAsync(new InviteCode
        {
            UserId = request.UserId,
            Value = code,
            ValidUntil = DateTime.UtcNow.AddDays(request.CallbackValidity)
        }, ctx);

        var baseUrl = _appSettings.BaseUrl.TrimEnd('/');
        request.CallBackUrl = $"{baseUrl}/events/invite-callback?code={code}&eventId={request.EventId}";
    }

    private static bool IsSendableEmail(string email) => !email.EndsWith($"@{Email.NonSendableEmail}");
}
