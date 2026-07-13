using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services;

public class NoOpMailService(ILogger<NoOpMailService> logger) : IMailService
{
    public Task SendInvitationAsync(InvitationEmailRequest request, CancellationToken ctx = default)
    {
        logger.LogDebug(
            """
            Fake invitation sent:
                Event: {EventName}
                Display Name: {DisplayName}
                Email: {Email}
                Callback URL: {CallBackUrl}
                Inlines: {InlineCount}
            """,
            request.EventName, request.UserName, request.UserEmail, request.CallBackUrl, request.InlineAttachments.Count());
        return Task.CompletedTask;
    }

    public Task SendSaveTheDateAsync(SaveDateEmailRequest request, CancellationToken ctx = default)
    {
        logger.LogDebug(
            """
            Fake Save the Date sent:
                Template ID: {TemplateId}
                Event: {EventName}
                Display Name: {DisplayName}
                Email: {Email},
                Inline Attachment Count: {InlineCount}
            """,
            request.TemplateId, request.EventName, request.UserName, request.UserEmail, request.InlineAttachments.Count());
        return Task.CompletedTask;
    }
}
