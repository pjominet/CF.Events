using CF.Events.Web.Infrastructure.HttpClients;
using CF.Events.Web.Infrastructure.Providers.Interfaces;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Infrastructure.Providers;

public class Smtp2GoEmailProvider(ISmtp2GoClient smtp2GoClient, IOptions<AppSettings> settings) : IEmailProvider
{
    private readonly EmailProviderSettings _emailSettings = settings.Value.EmailProviderSettings;

    public Task<bool> SendEmailAsync(EmailEntry templatedEmailEntry, CancellationToken ctx = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> SendTemplatedEmailAsync(TemplatedEmailEntry templatedEmailEntry, CancellationToken ctx = default)
    {
        var request = new Smtp2GoEmailRequest
        {
            TemplateId = templatedEmailEntry.TemplateId,
            Sender = _emailSettings.SenderEmail,
            To = [templatedEmailEntry.To],
            TemplateData = templatedEmailEntry.Variables,
            Inlines = templatedEmailEntry.Attachments?.Select(a => new Smtp2GoInlineAttachment
            {
                FileName = a.FileName,
                FileBlob = Convert.ToBase64String(a.Content),
                MimeType = a.ContentType
            }).ToList()
        };

        try
        {
            await smtp2GoClient.SendTemplatedEmailAsync(request, ctx);
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to send email via Smtp2go", ex);
        }
    }

    public Task<bool> SendEmailsAsync(IEnumerable<EmailEntry> emailEntries, CancellationToken ctx = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> SendTemplatedEmailsAsync(IEnumerable<TemplatedEmailEntry> emailEntries, CancellationToken ctx = default)
    {
        var bulkRequest = new Smtp2GoBulkEmailRequest
        {
            Emails =
            [
                .. emailEntries.Select(entry => new Smtp2GoEmailRequestItem
                {
                    TemplateId = entry.TemplateId,
                    Sender = _emailSettings.SenderEmail,
                    To = [entry.To],
                    TemplateData = entry.Variables,
                    Inlines = entry.Attachments?.Select(a => new Smtp2GoInlineAttachment
                    {
                        FileName = a.FileName,
                        FileBlob = Convert.ToBase64String(a.Content),
                        MimeType = a.ContentType
                    }).ToList()
                })
            ]
        };

        try
        {
            await smtp2GoClient.SendBulkTemplatedEmailsAsync(bulkRequest, ctx);
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to send bulk emails via Smtp2go", ex);
        }
    }
}
