namespace CF.Events.Web.Models.Requests;

public class SaveTheDateEmailRequest
{
    public required string TemplateId { get; init; }
    public required string EventName { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required string ReturnUrl { get; init; }
    public IEnumerable<EmailInlineAttachment> InlineAttachments { get; init; } = [];
}
