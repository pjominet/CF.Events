using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services;

public class NoOpMailService(ILogger<NoOpMailService> logger) : IMailService
{
    public Task SendTemplatedEmailAsync(TemplateEmailRequest request, CancellationToken ctx = default)
    {
        logger.LogDebug(
            """
            Fake {Type} sent:
                Template ID: {TemplateId}
                Send With Link : {SendWithLink}
                Event: {EventName}
                User Name: {UserName}
                Email: {Email}
                Deadline: {Deadline}
                Callback URL: {CallBackUrl}
                Inlines: {InlineCount}
            """,
            request.GetType().Name, request.TemplateId, request.SendWithLink, request.EventName, request.UserName, request.UserEmail, request.Deadline, request.CallBackUrl, request.InlineAttachments.Count());
        return Task.CompletedTask;
    }
}
