using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models.Requests;
using Microsoft.Extensions.Options;
using Smtp2Go.Api;
using Smtp2Go.Api.Models.Emails;

namespace CF.Events.Web.Services;

public class Smtp2GoEmailProvider(IApiService smtp2GoClient, IOptions<AppSettings> settings) : IEmailProvider
{
    private readonly EmailProviderSettings _settings = settings.Value.EmailProviderSettings;

    public async Task SendTemplatedEmailAsync(string templateId, string to, IDictionary<string, string> variables, IEnumerable<InlineAttachment>? inlineAttachments = null, CancellationToken ctx = default)
    {
        var message = new TemplatedEmailMessage(templateId, _settings.SenderEmail, to);

        foreach (var variable in variables)
            message.AddTemplateVariable(variable.Key, variable.Value);

        if (inlineAttachments is not null)
        {
            foreach (var attachment in inlineAttachments)
                message.AddInlineImage(attachment.FileName, Convert.ToBase64String(attachment.Content), attachment.ContentType);
        }

        try
        {
            var response = await smtp2GoClient.SendTemplatedEmail(message).ConfigureAwait(false);

            if (response.Data.Succeeded == 0)
            {
                var errors = response.Data.Failures != null ? string.Join(", ", response.Data.Failures) : response.Data.Error ?? "Unknown error";
                throw new Exception($"Smtp2go API error: {errors}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to send email via Smtp2go", ex);
        }
    }
}
