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

    public int? InviteCodeId { get; set; } // Can be null for direct invites without code

    [StringLength(100)]
    public string? GroupName { get; set; } // "The Smith Family", "John & Jane", etc.

    [StringLength(500)]
    public string? Notes { get; set; } // Internal notes for organizer

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; } // When to send the invitation email
    public bool InviteEmailSent { get; set; } = false; // Whether invitation email has been sent
    public string? AssignedAccommodationCode { get; set; } // Accommodation code for this invitation

    // Navigation properties
    public Event Event { get; set; } = null!;
    public InviteCode? InviteCode { get; set; }
    public HashSet<InvitedPerson> InvitedPersons { get; set; } = [];
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
