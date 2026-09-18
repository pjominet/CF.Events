using System.Text.Json;
using CF.Events.Web.Infrastructure.HttpClients;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Infrastructure.Providers;

public class Smtp2GoEmailProvider(ISmtp2GoClient smtp2GoClient, IOptions<AppSettings> settings) : IEmailProvider
{
    private readonly EmailProviderSettings _emailSettings = settings.Value.EmailProviderSettings;

    public async Task<bool> SendTemplatedEmailAsync(EmailEntry emailEntry, CancellationToken ctx = default)
    {
        var request = new Smtp2GoEmailRequest
        {
            ApiKey = _emailSettings.Smtp2Go.ApiKey,
            TemplateId = emailEntry.TemplateId,
            Sender = _emailSettings.SenderEmail,
            To = [emailEntry.To],
            TemplateData = emailEntry.Variables,
            Inlines = emailEntry.InlineAttachments?.Select(a => new Smtp2GoInlineAttachment
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

    public async Task<bool> SendTemplatedEmailsBulkAsync(IEnumerable<EmailEntry> emailEntries, CancellationToken ctx = default)
    {
        var bulkRequest = new Smtp2GoBulkEmailRequest
        {
            ApiKey = _emailSettings.Smtp2Go.ApiKey,
            Emails =
            [
                .. emailEntries.Select(entry => new Smtp2GoEmailRequestItem
                {
                    TemplateId = entry.TemplateId,
                    Sender = _emailSettings.SenderEmail,
                    To = [entry.To],
                    TemplateData = entry.Variables,
                    Inlines = entry.InlineAttachments?.Select(a => new Smtp2GoInlineAttachment
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
