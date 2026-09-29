using CF.Events.Web.Infrastructure.Providers.Interfaces;
using CF.Events.Web.Models.Requests;
using CF.Events.Web.Services.Interfaces;

namespace CF.Events.Web.Services.EmailSenders;

public class EmailSender(IEmailProvider emailProvider) : IEmailSender
{
    public async Task SendTemplatedEmailAsync(TemplateEmailRequest request, CancellationToken ctx = default)
    {
        var variables = request.BuildTemplateVariables();
        var emailData = new TemplatedEmailEntry
        (
            request.TemplateId,
            request.UserEmail,
            variables,
            request.EmailAttachments
        );
        await emailProvider.SendTemplatedEmailAsync(emailData, ctx);
    }

    public async Task SendTemplatedEmailsAsync(IEnumerable<TemplateEmailRequest> requests, CancellationToken ctx = default)
    {
        var entries = requests.Select(request => new TemplatedEmailEntry(
            request.TemplateId,
            request.UserEmail,
            request.BuildTemplateVariables(),
            request.EmailAttachments
        ));
        await emailProvider.SendTemplatedEmailsAsync(entries, ctx);
    }
}
