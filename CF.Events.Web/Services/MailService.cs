using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services;

public class MailService(IEmailProvider emailProvider) : IMailService
{
    public async Task SendInvitationAsync(InvitationEmailRequest request, CancellationToken ctx = default)
    {
        var variables = new Dictionary<string, string>
        {
            { "sender_sig", "Patrick & Éadaoin" },
            { "invitation_callback", request.CallBackUrl },
            { "user_name", request.UserName },
            { "event_name", request.EventName }
        };

        await emailProvider.SendTemplatedEmailAsync(request.TemplateId, request.UserEmail, variables, request.InlineAttachments, ctx);
    }

    public async Task SendSaveTheDateAsync(SaveDateEmailRequest request, CancellationToken ctx = default)
    {
        var variables = new Dictionary<string, string>
        {
            { "sender_sig", "Patrick & Éadaoin" },
            { "event_date", request.EventStartDate }
        };

        await emailProvider.SendTemplatedEmailAsync(request.TemplateId, request.UserEmail, variables, request.InlineAttachments, ctx);
    }

    public async Task SendSaveTheDateWithLinkAsync(SaveDateEmailRequest request, CancellationToken ctx = default)
    {
        var variables = new Dictionary<string, string>
        {
            { "sender_sig", "Patrick & Éadaoin" },
            { "user_name", request.UserName },
            { "invite_url", request.CallBackUrl },
            { "event_date", request.EventStartDate }
        };

        await emailProvider.SendTemplatedEmailAsync(request.TemplateId, request.UserEmail, variables, [], ctx);
    }
}
