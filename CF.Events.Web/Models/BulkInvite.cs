namespace CF.Events.Web.Models;

public class BulkInvite
{
    public required List<string> UserIds { get; init; }
    public required string InviteCode { get; init; }
    public bool SendEmailsOnInvite { get; init; }
    public bool AllowUseOfAccommodationCode { get; init; }
}
