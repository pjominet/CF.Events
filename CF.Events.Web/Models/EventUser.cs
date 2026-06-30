using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class EventUser
{
    [StringLength(450)]
    public required string UserId { get; set; }
    public required int EventId { get; set; }
    [StringLength(100)]
    public string? AssignedAccommodationCode { get; set; }

    public int InviteCodeId { get; set; }
    public bool InviteEmailSent { get; set; }
    public DateTime? ScheduledFor { get; set; }

    // navigation properties
    public Event Event { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public InviteCode InviteCode { get; set; } = null!;
    public Rsvp? Rsvp { get; set; }
}
