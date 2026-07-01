using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// DTO for inviting users to an event (replaces legacy UserInvites)
/// </summary>
public class InviteUsersRequest
{
    [Required]
    public List<string> UserIds { get; init; } = [];

    [Required]
    public int InviteCodeId { get; init; }

    public bool SendEmailsOnInvite { get; init; } = true;

    public DateTime? ScheduledFor { get; init; }

    public bool AllowUseOfAccommodationCode { get; init; } = true;
}
