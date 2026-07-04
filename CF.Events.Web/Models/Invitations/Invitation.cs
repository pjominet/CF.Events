using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// Represents a group invitation to an event.
/// One invitation can include multiple people (couples, families, groups).
/// </summary>
public class Invitation
{
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }



    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; } // When to send the invitation email
    public bool InviteEmailSent { get; set; } // Whether invitation email has been sent

    [StringLength(100)]
    public string? AssignedAccommodationCode { get; set; } // Accommodation code for this invitation

    // Navigation properties
    public Event Event { get; set; } = null!;

    public HashSet<InviteGroup> InvitedPersons { get; set; } = [];
    public Rsvp? Rsvp { get; set; }
}

public enum InvitationStatus
{
    Pending,
    Sent,
    Viewed,
    Responded,
    Cancelled
}
