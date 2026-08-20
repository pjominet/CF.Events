namespace CF.Events.Web.Models.Requests;

public abstract class TemplateEmailRequest
{
    protected const string AppName = "P&E Wedding";
    public required string TemplateId { get; init; }
    public required string SenderName { get; set; }
    public required string SenderEmail { get; set; }
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

    public abstract Dictionary<string, string> BuildTemplateVariables();
}

public class InvitationEmailRequest : TemplateEmailRequest
{
    public override Dictionary<string, string> BuildTemplateVariables() => new()
    {
        { "sender_sig", SenderName },
        { "app_name", AppName },
        { "invite_url", CallBackUrl },
        { "user_name", UserName },
        { "event_name", EventName },
        { "event_date", EventDate },
        { "deadline", DateTime.UtcNow.AddDays(CallbackValidity).ToLongDateString() },
        { "reply-email", SenderEmail }
    };
}

public class SaveDateEmailRequest : TemplateEmailRequest
{
    public override Dictionary<string, string> BuildTemplateVariables()
    {
        var variables = new Dictionary<string, string>
        {
            { "sender_sig", SenderName },
            { "app_name", AppName },
            { "event_date", EventDate }
        };

        if (SendWithLink)
        {
            variables["user_name"] = UserName;
            variables["invite_url"] = CallBackUrl;
            variables["event_name"] = EventName;
        }

        return variables;
    }
}

