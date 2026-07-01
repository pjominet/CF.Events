namespace CF.Events.Web.Models.Requests;

public record InviteEmailRequest
{
    public int EventId { get; init; }
    public required string UserId { get; init; }
    public required string EventName { get; init; }
    public required string UserDisplayName { get; init; }
    public required string UserEmail { get; init; }
    public string? InviteCode { get; init; }
}
