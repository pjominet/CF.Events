namespace CF.Events.Web.Models.Requests;

public record InviteeNotesRequest
{
    public required string UserId { get; init; }
    public string? Notes { get; init; }
};
