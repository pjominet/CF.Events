using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services;

public interface IEmailProvider
{
    Task<bool> SendTemplatedEmailAsync(EmailEntry emailEntry, CancellationToken ctx = default);
    Task<bool> SendTemplatedEmailsBulkAsync(IEnumerable<EmailEntry> emailEntries, CancellationToken ctx = default);
}

public record EmailEntry(string TemplateId, string To, IDictionary<string, string> Variables, IEnumerable<InlineAttachment>? InlineAttachments = null);
