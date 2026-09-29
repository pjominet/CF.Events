using CF.Events.Web.Models.Requests;
using CF.Events.Web.Services.Interfaces;

namespace CF.Events.Web.Services.EmailSenders;

public class NoOpEmailSender(ILogger<NoOpEmailSender> logger) : IEmailSender
{
    public Task SendTemplatedEmailAsync(TemplateEmailRequest request, CancellationToken ctx = default)
    {
        LogRequest(request);
        return Task.CompletedTask;
    }

    public Task SendTemplatedEmailsAsync(IEnumerable<TemplateEmailRequest> requests, CancellationToken ctx = default)
    {
        foreach (var request in requests) LogRequest(request);
        return Task.CompletedTask;
    }

    private void LogRequest(TemplateEmailRequest request)
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
            request.GetType().Name, request.TemplateId, request.SendWithLink, request.EventName, request.UserName, request.UserEmail, request.Deadline, request.CallBackUrl, request.EmailAttachments.Count());
    }
}
