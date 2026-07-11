namespace CF.Events.Web.Models.Requests;

public class InvitationEmailRequest : IEmailRequest
{
    public required string TemplateId { get; init; }
    public int EventId { get; init; }
    public required string EventName { get; init; }
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required string UserEmail { get; init; }
    public string? InviteCode { get; init; }
    public required string CallBackUrl { get; init; }
    public IEnumerable<InlineAttachment> InlineAttachments { get; init; } = [];
}
