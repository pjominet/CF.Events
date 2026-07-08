namespace CF.Events.Web.Models.Requests;

public interface IEmailRequest
{
    int EventId { get; }
    string EventName { get; }
    string UserId { get; }
    string UserName { get; }
    string UserEmail { get; }
    IEnumerable<InlineAttachment> InlineAttachments { get; }
}
