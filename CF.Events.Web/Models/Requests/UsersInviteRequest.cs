using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models.Requests;

public record UsersInviteRequest
{
    public List<string> UserIds { get; init; } = [];
    [Required]
    public int InviteCodeId { get; init; }
    public bool SendEmailsOnInvite { get; init; }
    public DateTime? ScheduledFor { get; init; }
    public bool AllowAccommodationCode { get; init; }
    public string? SelectedAccommodationCode { get; init; }
}
