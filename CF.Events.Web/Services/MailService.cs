using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services;

public class MailService(IEmailProvider emailProvider) : IMailService
{
    public async Task SendTemplatedEmailAsync(TemplateEmailRequest request, CancellationToken ctx = default)
    {
        var variables = request.BuildTemplateVariables();
        await emailProvider.SendTemplatedEmailAsync(request.TemplateId, request.UserEmail, variables, request.InlineAttachments, ctx);
    }
}
