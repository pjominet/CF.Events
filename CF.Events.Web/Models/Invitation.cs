namespace CF.Events.Web.Models;

public record Invitation
{
    public int EventId { get; init; }
    public string UserId { get; init; }
    public string EventName { get; init; }
    public string UserDisplayName { get; init; }
    public string UserEmail { get; init; }
    public string? InviteCode { get; init; }
}
