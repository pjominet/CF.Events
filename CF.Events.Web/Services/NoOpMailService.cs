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
            request.EventName, request.UserName, request.Email, request.CallBackUrl, request.InlineAttachments.Count());
        return Task.CompletedTask;
    }

    public Task SendSaveTheDateAsync(SaveTheDateEmailRequest request, CancellationToken ctx = default)
    {
        logger.LogDebug(
            """
            Fake Save the Date sent:
                Template ID: {TemplateId}
                Event: {EventName}
                Display Name: {DisplayName}
                Email: {Email},
                Return URL: {ReturnUrl}
                Inlines: {InlineCount}
            """,
            request.TemplateId, request.EventName, request.UserName, request.Email, request.ReturnUrl, request.InlineAttachments.Count());
        return Task.CompletedTask;
    }
}
