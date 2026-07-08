using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services;

public interface IEmailProvider
{
    Task SendTemplatedEmailAsync(string templateId, string to, IDictionary<string, string> variables, IEnumerable<InlineAttachment>? inlineAttachments = null, CancellationToken ctx = default);
}
