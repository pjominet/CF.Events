using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// Represents a group RSVP response for an invitation.
/// One RSVP contains responses for multiple people (RsvpPerson).
/// </summary>
public class Rsvp
{
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    public int InvitationId { get; set; } // Links to the invitation group

    public RsvpStatus Status { get; set; } = RsvpStatus.InProgress;

    [StringLength(500)]
    public string? Comments { get; set; }

    // Kids count per age bracket (simplified - no individual kid details needed)
    public Dictionary<KidAgeBracket, int>? KidsDetails { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    // Group info - can override from Invitation
    [StringLength(100)]
    public string? GroupName { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public Invitation Invitation { get; set; } = null!;
    public HashSet<RsvpPerson> People { get; set; } = [];
    public HashSet<RsvpCustomAnswer> CustomAnswers { get; set; } = [];
}

public enum RsvpStatus
{
    InProgress,
    Submitted,
    Updated,
    Cancelled
}

// === Legacy Enums - Kept for backward compatibility ===
// These will be moved to separate files in future refactoring

public enum DietaryOptions
{
    None,
    Vegetarian,
    Vegan,
    Pescetarian,
    GlutenIntolerant,
    DairyIntolerant,
    LactoseIntolerant,
}

public enum KidAgeBracket
{
    ZeroToThree,
    FourToEight,
    NineToFifteen,
    SixteenOrOlder
}
