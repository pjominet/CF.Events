using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services;

public class MailService(IEmailProvider emailProvider) : IMailService
{
    public async Task SendTemplatedEmailAsync(TemplateEmailRequest request, CancellationToken ctx = default)
    {
        var variables = request.BuildTemplateVariables();
        var emailData = new EmailEntry
        (
            request.TemplateId,
            request.UserEmail,
            variables,
            request.InlineAttachments
        );
        await emailProvider.SendTemplatedEmailAsync(emailData, ctx);
    }

    public async Task SendTemplatedEmailsBulkAsync(IEnumerable<TemplateEmailRequest> requests, CancellationToken ctx = default)
    {
        var entries = requests.Select(request => new EmailEntry(
            request.TemplateId,
            request.UserEmail,
            request.BuildTemplateVariables(),
            request.InlineAttachments
        ));
        await emailProvider.SendTemplatedEmailsBulkAsync(entries, ctx);
    }
}
