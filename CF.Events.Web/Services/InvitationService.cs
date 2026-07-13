using System.Linq.Expressions;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Services;

public interface IInvitationService
{
    AppSettings AppSettings { get; }
    Task<int> ProcessPendingEmails(CancellationToken ctx = default);
    Task SendImmediateEmails<T>(List<T> requests, CancellationToken ctx = default) where T : class, IEmailRequest;
    Task SendEmail<T>(T request, CancellationToken ctx = default) where T : class, IEmailRequest;
    Task PrepareInvitationAsync(IEmailRequest request, CancellationToken ctx = default);
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

        await SendImmediateEmails(pending, ctx);

        return pending.Count;
    }

    public async Task PrepareInvitationAsync(IEmailRequest request, CancellationToken ctx = default)
    {
        var code = CodeGenerator.Generate(32);
        await db.InviteCodes.AddAsync(new InviteCode
        {
            UserId = request.UserId,
            Value = code,
            ValidUntil = request.CallbackValidity
        }, ctx);

        var baseUrl = _appSettings.BaseUrl.TrimEnd('/');
        request.CallBackUrl = $"{baseUrl}/events/invite-callback?code={code}";
    }

    public async Task SendImmediateEmails<T>(List<T> requests, CancellationToken ctx = default) where T : class, IEmailRequest
    {
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
}
