using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public record UserInvites
{
    public required List<string> UserIds { get; init; }
    [Required]
    public int InviteCodeId { get; init; }
    public bool SendEmailsOnInvite { get; init; }
    public DateTime? ScheduledFor { get; init; }
    public bool AllowUseOfAccommodationCode { get; init; }
}
