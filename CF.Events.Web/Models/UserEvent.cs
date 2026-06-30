using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class UserEvent
{
    public required string UserId { get; set; }
    public required int EventId { get; set; }
    [StringLength(100)]
    public string? AssignedAccommodationCode { get; set; }

    public bool InvitationEmailSent { get; set; }
    public DateTime? ScheduledFor { get; set; }

    [StringLength(100)]
    public string? InvitationInviteCode { get; set; }

    // navigation properties
    public Event Event { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public Rsvp? Rsvp { get; set; }
}
