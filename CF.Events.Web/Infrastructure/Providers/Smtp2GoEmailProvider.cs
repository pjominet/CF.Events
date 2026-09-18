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

    public async Task SendTemplatedEmailAsync(EmailEntry emailEntry, CancellationToken ctx = default)
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
            var response = await smtp2GoClient.SendTemplatedEmailAsync(request, ctx);
            ProcessApiResponse(response);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to send email via Smtp2go", ex);
        }
    }

    public async Task SendTemplatedEmailsBulkAsync(IEnumerable<EmailEntry> emailEntries, CancellationToken ctx = default)
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
            var response = await smtp2GoClient.SendBulkTemplatedEmailsAsync(bulkRequest, ctx);
            ProcessApiResponse(response);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to send bulk emails via Smtp2go", ex);
        }
    }

    private static void ProcessApiResponse(Smtp2GoApiResponse response)
    {
        // Success is determined by HTTP status code in Smtp2GoClient.
        // Verify that the data is not empty and doesn't contain hidden errors.
        if (response.Data.ValueKind is not JsonValueKind.Object) return;

        if (!response.Data.TryGetProperty("failed", out var failedProp) || !failedProp.TryGetInt32(out var failedCount) || failedCount <= 0)
            return;

        var errorMessage = "Some emails failed to send.";
        if (response.Data.TryGetProperty("failures", out var failuresProp) && failuresProp.ValueKind is JsonValueKind.Array)
            errorMessage += $" Failures: {failuresProp}";

        throw new Exception($"Smtp2go partial success: {errorMessage} (Request ID: {response.RequestId})");
    }
}
