using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models.Requests;

public record UsersInviteRequest
{
    [Required]
    public List<string> UserIds { get; init; } = [];

    [Required]
    public int InviteCodeId { get; init; }

    public SendEmailAction SendEmailsOnInvite { get; init; } = SendEmailAction.NoSend;
    public DateTime? ScheduledFor { get; init; }
    public bool AllowAccommodationCode { get; init; }
    public string? SelectedAccommodationCode { get; init; }
}

public enum SendEmailAction
{
    NoSend,
    Immediately,
    Scheduled
}
