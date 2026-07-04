using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// Represents a person's RSVP response within a group RSVP.
/// Each person in an invitation group has their own RsvpPerson entry.
/// </summary>
public class RsvpPerson
{
    public int Id { get; set; }

    [Required]
    public int RsvpId { get; set; }

    public int? InvitedPersonId { get; set; } // Links to invited person, null for ad-hoc plus ones

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Email { get; set; }

    public bool IsPlusOne { get; set; } = false;
    public bool IsPrimary { get; set; } = false; // Primary invitee in the group

    public bool Attending { get; set; } = true;

    // Navigation properties
    public Rsvp Rsvp { get; set; } = null!;
    public InviteGroup? InvitedPerson { get; set; }
    public HashSet<RsvpFoodPreference> FoodPreferences { get; set; } = [];
    public HashSet<RsvpAccommodation> Accommodations { get; set; } = [];
}
