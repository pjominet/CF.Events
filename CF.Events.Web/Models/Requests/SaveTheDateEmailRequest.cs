namespace CF.Events.Web.Models.Requests;

public class SaveTheDateEmailRequest : IEmailRequest
{
    public required string TemplateId { get; init; }
    public int EventId { get; set; }
    public required string EventName { get; init; }
    public required string UserId { get; set; }
    public required string UserName { get; init; }
    public required string UserEmail { get; init; }
    public IEnumerable<InlineAttachment> InlineAttachments { get; init; } = [];
}
