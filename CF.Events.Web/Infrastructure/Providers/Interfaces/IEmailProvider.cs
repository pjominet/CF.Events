using CF.Events.Web.Models.Requests;
using Microsoft.AspNetCore.StaticFiles;

namespace CF.Events.Web.Infrastructure.Providers.Interfaces;

public interface IEmailProvider
{
    Task<bool> SendEmailAsync(EmailEntry templatedEmailEntry, CancellationToken ctx = default);
    Task<bool> SendTemplatedEmailAsync(TemplatedEmailEntry templatedEmailEntry, CancellationToken ctx = default);
    Task<bool> SendEmailsAsync(IEnumerable<EmailEntry> emailEntries, CancellationToken ctx = default);
    Task<bool> SendTemplatedEmailsAsync(IEnumerable<TemplatedEmailEntry> emailEntries, CancellationToken ctx = default);
}

public class EmailAttachment(string fileName, string contentType, byte[] content)
{
    public string FileName { get; init; } = fileName;
    public string ContentType { get; init; } = contentType;
    public byte[] Content { get; init; } = content;

    public static EmailAttachment BuildInlineImage(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Image file not found", filePath);

        var fileName = Path.GetFileName(filePath);
        new FileExtensionContentTypeProvider().TryGetContentType(fileName, out var contentType);
        return new EmailAttachment(fileName, contentType ?? "application/octet-stream", File.ReadAllBytes(filePath));
    }
}

public record EmailEntry(string To, string Subject, string Body, IEnumerable<EmailAttachment>? Attachments = null);

public record TemplatedEmailEntry(string TemplateId, string To, IDictionary<string, string> Variables, IEnumerable<EmailAttachment>? Attachments = null);
