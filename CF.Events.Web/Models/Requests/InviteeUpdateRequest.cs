namespace CF.Events.Web.Models.Requests;

public record InviteeUpdateRequest
{
    public string UserId { get; init; } = string.Empty;
    public string? AccommodationCode { get; init; }
    public ushort? Priority { get; init; }
}
