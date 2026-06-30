using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class UserEvent
{
    public required string UserId { get; set; }
    public required int EventId { get; set; }
    [StringLength(100)]
    public string? AssignedAccommodationCode { get; set; }

    // navigation properties
    public Event Event { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public Rsvp? Rsvp { get; set; }
}
