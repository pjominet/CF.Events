namespace CF.Events.Web.Models.Requests;
public interface ITemplateEmailRequest
{
    public string TemplateId { get; }
    public bool SendWithLink { get; set; }
    public int EventId { get; }
    public string EventName { get; }
    public string UserId { get; }
    public string UserName { get; }
    public string UserEmail { get; }
    public int CallbackValidity { get; }
    public string CallBackUrl { get; set; }
    IEnumerable<InlineAttachment> InlineAttachments { get; }
}

public class InvitationEmailRequest : ITemplateEmailRequest
{
    public required string TemplateId { get; init; }
    public bool SendWithLink { get; set; }
    public int EventId { get; init; }
    public required string EventName { get; init; }
    public required string EventDate { get; init; }
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required string UserEmail { get; init; }
    public int CallbackValidity { get; init; }
    public string CallBackUrl { get; set; } = string.Empty;
    public IEnumerable<InlineAttachment> InlineAttachments { get; set; } = [];
}

public class SaveDateEmailRequest : ITemplateEmailRequest
{
    public required string TemplateId { get; init; }
    public bool SendWithLink { get; set; }
    public int EventId { get; init; }
    public required string EventName { get; init; }
    public required string EventStartDate { get; init; }
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required string UserEmail { get; init; }
    public int CallbackValidity { get; init; }
    public string CallBackUrl { get; set; } = string.Empty;
    public IEnumerable<InlineAttachment> InlineAttachments { get; set; } = [];
}

public class LoginEmailRequest
{
    public bool SendWithLink { get; set; } = true;
    public int? EventId { get; init; }
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required string UserEmail { get; init; }
    public string CallBackUrl { get; set; } = string.Empty;
}
