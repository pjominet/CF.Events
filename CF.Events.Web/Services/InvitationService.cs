using System.Linq.Expressions;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Services;

public interface IInvitationService
{
    Task<int> ProcessPendingEmails(CancellationToken ctx = default);
    Task<int> InviteUsersAsync(int eventId, UsersInviteRequest inviteRequest, CancellationToken ctx = default);
    Task SendInvitesAsync(int eventId, List<string> userIds, CancellationToken ctx = default);
    Task<EmailSendResult> SendSaveTheDateAsync(int eventId, string userId, CancellationToken ctx = default);
    Task<EmailSendResult> SendBulkSaveTheDateAsync(int eventId, List<string> userIds, CancellationToken ctx = default);
}

public class InvitationService(
    EventsDbContext db,
    IMailService mailService,
    IAuthEmailService authEmailService,
    IFileService fileService,
    IOptions<AppSettings> appOptions,
    ILogger<InvitationService> logger) : IInvitationService
{
    private readonly AppSettings _appSettings = appOptions.Value;

    public async Task<int> ProcessPendingEmails(CancellationToken ctx = default)
    {
        var sentInvitations = await ProcessPendingType<InvitationEmailRequest>(
            ue => ue.InviteEmailSent == null && ue.ScheduledFor != null && ue.ScheduledFor <= DateTime.UtcNow,
            ue => new InvitationEmailRequest
            {
                SenderName = _appSettings.EmailProviderSettings.SenderName,
                SenderEmail = _appSettings.EmailProviderSettings.SenderEmail,
                EventId = ue.EventId,
                UserId = ue.UserId,
                EventName = ue.Event.Name,
                EventDate = ue.Event.StartDate.ToLongDateString(),
                UserName = ue.User.DisplayName!,
                UserEmail = ue.User.Email!,
                TemplateId = ue.Event.InvitationTemplateId ?? string.Empty,
                CallbackValidity = ue.Event.InviteValidity
            }, ctx);

        var sentSaveTheDates = await ProcessPendingType<SaveDateEmailRequest>(
            ue => ue.SaveTheDateEmailSent == null && ue.ScheduledFor != null && ue.ScheduledFor <= DateTime.UtcNow,
            ue => new SaveDateEmailRequest
            {
                SenderName = _appSettings.EmailProviderSettings.SenderName,
                SenderEmail = _appSettings.EmailProviderSettings.SenderEmail,
                EventId = ue.EventId,
                UserId = ue.UserId,
                EventName = ue.Event.Name,
                EventDate = ue.Event.StartDate.ToLongDateString(),
                UserName = ue.User.DisplayName!,
                UserEmail = ue.User.Email!,
                TemplateId = ue.Event.SaveDateTemplateId ?? string.Empty,
                SendWithLink = ue.Event.EmailWithLink
            }, ctx);

        return sentInvitations + sentSaveTheDates;
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
            ScheduledFor = inviteRequest.ScheduledFor
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
                SenderName = _appSettings.EmailProviderSettings.SenderName,
                SenderEmail = _appSettings.EmailProviderSettings.SenderEmail,
                EventId = ue.EventId,
                UserId = ue.UserId,
                EventName = ue.Event.Name,
                EventDate = ue.Event.StartDate.ToLongDateString(),
                UserName = ue.User.DisplayName!,
                UserEmail = ue.User.Email!,
                CallbackValidity = ue.Event.InviteValidity
            })
            .ToListAsync(ctx);

        foreach (var newInvitation in newInvitations)
        {
            await PrepareEmailRequestAsync(newInvitation, ctx);
        }

        await db.SaveChangesAsync(ctx);

        await SendBatchedEmails(newInvitations, ctx);

        return count;
    }

    public async Task SendInvitesAsync(int eventId, List<string> userIds, CancellationToken ctx = default)
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
            .Select(e => new { e.Id, e.Name, e.StartDate, e.InvitationTemplateId, e.InviteValidity })
            .FirstOrDefaultAsync(ctx);

        if (eventData is null)
            throw new ArgumentException("Event does not exist");

        var requests = new List<InvitationEmailRequest>();

        foreach (var user in eventUsers)
        {
            var request = new InvitationEmailRequest
            {
                TemplateId = eventData.InvitationTemplateId ?? string.Empty,
                SenderName = _appSettings.EmailProviderSettings.SenderName,
                SenderEmail = _appSettings.EmailProviderSettings.SenderEmail,
                EventId = eventData.Id,
                EventName = eventData.Name,
                EventDate = eventData.StartDate.ToLongDateString(),
                UserName = user.DisplayName!,
                UserEmail = user.Email!,
                UserId = user.UserId,
                CallbackValidity = eventData.InviteValidity
            };

            await PrepareEmailRequestAsync(request, ctx);
            requests.Add(request);
        }

        await db.SaveChangesAsync(ctx);

        if (requests.Count > 0)
            await SendBatchedEmails(requests, ctx);
    }

    public async Task<EmailSendResult> SendSaveTheDateAsync(int eventId, string userId, CancellationToken ctx = default)
    {
        var @event = await db.Events.FindAsync([eventId], ctx);
        if (@event is null)
            return new EmailSendResult(EmailSendResultStatus.EventNotFound, Message: "Event not found");

        if (!@event.SaveDateTemplateId.HasValue(false))
            return new EmailSendResult(EmailSendResultStatus.TemplateMissing, Message: "Event is not eligible for Save the Date (no template ID set)");

        var user = await db.Users
            .Where(u => u.IsActive && u.Id == userId)
            .FirstOrDefaultAsync(ctx);
        if (user is null)
            return new EmailSendResult(EmailSendResultStatus.UserNotFound, Message: "User not found");

        var request = BuildSaveDateRequest(@event, user.Id, user.DisplayName!, user.Email!);
        await SendEmail(request, ctx);

        return new EmailSendResult(EmailSendResultStatus.Success, Message: user.DisplayName);
    }

    public async Task<EmailSendResult> SendBulkSaveTheDateAsync(int eventId, List<string> userIds, CancellationToken ctx = default)
    {
        if (userIds.Count == 0)
            return new EmailSendResult(EmailSendResultStatus.UserNotFound, Message: "No users selected");

        var @event = await db.Events.FindAsync([eventId], ctx);
        if (@event is null)
            return new EmailSendResult(EmailSendResultStatus.EventNotFound, Message: "Event not found");

        if (!@event.SaveDateTemplateId.HasValue(false))
            return new EmailSendResult(EmailSendResultStatus.TemplateMissing, Message: "Event is not eligible for Save the Date (no template ID set)");

        var eligibleUsers = await db.EventUsers
            .Where(eu => eu.EventId == eventId && userIds.Contains(eu.UserId) && eu.User.IsActive)
            .Select(eu => new { eu.UserId, DisplayName = eu.User.DisplayName!, Email = eu.User.Email! })
            .ToListAsync(ctx);

        if (eligibleUsers.Count == 0)
            return new EmailSendResult(EmailSendResultStatus.UserNotFound, Message: "No users found or eligible for Save the Date");

        var requests = eligibleUsers
            .Select(u => BuildSaveDateRequest(@event, u.UserId, u.DisplayName, u.Email))
            .ToList();

        await SendBatchedEmails(requests, ctx);

        return new EmailSendResult(EmailSendResultStatus.Success, requests.Count);
    }

    private async Task<int> ProcessPendingType<T>(
        Expression<Func<EventUser, bool>> predicate,
        Expression<Func<EventUser, T>> selector,
        CancellationToken ctx) where T : TemplateEmailRequest
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
            await PrepareEmailRequestAsync(request, ctx);
        }

        await db.SaveChangesAsync(ctx);

        logger.LogInformation("Processing {Count} pending {Type} emails", pending.Count, typeof(T).Name);

        await SendBatchedEmails(pending, ctx);

        return pending.Count;
    }

    private async Task SendBatchedEmails<T>(List<T> requests, CancellationToken ctx = default) where T : TemplateEmailRequest
    {
        // filter out requests with non-sendable email addresses
        requests = [.. requests.Where(r => IsSendableEmail(r.UserEmail))];

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

            toSendImmediately = [.. requests.Take(batchSize)];
        }

        foreach (var request in toSendImmediately)
        {
            if (ctx.IsCancellationRequested) break;
            await SendEmail(request, ctx);
        }
    }

    private async Task SendEmail<T>(T request, CancellationToken ctx = default) where T : TemplateEmailRequest
    {
        try
        {
            if (!request.TemplateId.HasValue(false))
            {
                logger.LogWarning("No template ID found for event {EventId}", request.EventId);
                return;
            }

            await mailService.SendTemplatedEmailAsync(request, ctx);

            switch (request)
            {
                case InvitationEmailRequest inv:
                    await db.EventUsers
                        .Where(ue => ue.EventId == inv.EventId && ue.UserId == inv.UserId)
                        .ExecuteUpdateAsync(s => s.SetProperty(ue => ue.InviteEmailSent, DateTime.UtcNow), ctx);
                    break;
                case SaveDateEmailRequest std:
                    await db.EventUsers
                        .Where(ue => ue.EventId == std.EventId && ue.UserId == std.UserId)
                        .ExecuteUpdateAsync(s => s.SetProperty(ue => ue.SaveTheDateEmailSent, DateTime.UtcNow), ctx);
                    break;
            }

            logger.LogInformation("Sent {Type} email to {Email} for event {EventName}", typeof(T).Name, request.UserEmail, request.EventName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send {Type} email to {Email}", typeof(T).Name, request.UserEmail);
        }
    }


    private SaveDateEmailRequest BuildSaveDateRequest(Event @event, string userId, string userName, string userEmail)
    {
        var request = new SaveDateEmailRequest
        {
            TemplateId = @event.SaveDateTemplateId!,
            SenderName = _appSettings.EmailProviderSettings.SenderName,
            SenderEmail = _appSettings.EmailProviderSettings.SenderEmail,
            SendWithLink = @event.EmailWithLink,
            EventId = @event.Id,
            EventName = @event.Name,
            EventDate = @event.StartDate.ToLongDateString(),
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail
        };

        if (request.SendWithLink)
            request.CallBackUrl = BuildSaveDateCallbackUrl(request.EventId, request.UserId);
        else request.InlineAttachments = [fileService.GetAssetAttachment("save-the-date.png")];

        return request;
    }

    private async Task PrepareEmailRequestAsync(TemplateEmailRequest request, CancellationToken ctx = default)
    {
        switch (request)
        {
            case InvitationEmailRequest inv:
                var code = await authEmailService.CreateAuthCodeAsync(inv.UserId, inv.EventId, inv.CallbackValidity, ctx);
                inv.CallBackUrl = authEmailService.BuildAuthCallbackUrl(code, inv.EventId);
                break;
            case SaveDateEmailRequest std:
                if (std.SendWithLink)
                    std.CallBackUrl = BuildSaveDateCallbackUrl(std.EventId, std.UserId);
                else std.InlineAttachments = [fileService.GetAssetAttachment("save-the-date.png")];
                break;
        }
    }

    private string BuildSaveDateCallbackUrl(int eventId, string userId)
    {
        var baseUrl = _appSettings.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/events/{eventId}/{userId}/save-the-date";
    }

    private static bool IsSendableEmail(string email) => !email.EndsWith($"@{Email.NonSendableEmail}");
}
