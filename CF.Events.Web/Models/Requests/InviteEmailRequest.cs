namespace CF.Events.Web.Models;

public class InviteEmailRequest
{
    public int EventId { get; set; }
    public required string EventName { get; set; }
    public required string UserId { get; set; }
    public required string UserDisplayName { get; set; }
    public required string UserEmail { get; set; }
    public required string InvitationToken { get; set; }
}
