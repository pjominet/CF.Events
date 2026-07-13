namespace CF.Events.Web.Models.Requests;
public interface IEmailRequest
{
    public string TemplateId { get; }
    public int EventId { get; }
    public string EventName { get; }
    public string UserId { get; }
    public string UserName { get; }
    public string UserEmail { get; }
    public int CallbackValidity { get; }
    public string CallBackUrl { get; set; }
    IEnumerable<InlineAttachment> InlineAttachments { get; }
}

public class InvitationEmailRequest : IEmailRequest
{
    public required string TemplateId { get; init; }
    public int EventId { get; init; }
    public required string EventName { get; init; }
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required string UserEmail { get; init; }
    public int CallbackValidity { get; init; }
    public string CallBackUrl { get; set; } = string.Empty;
    public IEnumerable<InlineAttachment> InlineAttachments { get; set; }
}

public class SaveDateEmailRequest : IEmailRequest
{
    public required string TemplateId { get; init; }
    public int EventId { get; init; }
    public required string EventName { get; init; }
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required string UserEmail { get; init; }
    public int CallbackValidity { get; init; }
    public string CallBackUrl { get; set; } = string.Empty;
    public IEnumerable<InlineAttachment> InlineAttachments { get; set; }
}
