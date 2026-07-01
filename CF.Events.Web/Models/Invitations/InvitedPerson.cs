using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// Represents an individual person within a group invitation.
/// </summary>
public class InvitedPerson
{
    public int Id { get; set; }

    [Required]
    public int InvitationId { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; } // Null for non-registered guests

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Email { get; set; }

    public bool IsPrimary { get; set; } = false; // Main contact for the group

    public PersonInviteStatus Status { get; set; } = PersonInviteStatus.Pending;

    [StringLength(100)]
    public string? AssignedAccommodationCode { get; set; } // Accommodation code assigned to this person

    [StringLength(128)]
    public string? InvitationToken { get; set; } // Per-user, single-use, opaque token for invitation callback

    public DateTime? InvitationTokenExpiresAt { get; set; } // Expiry for the invitation token

    // Navigation properties
    public Invitation Invitation { get; set; } = null!;
    public AppUser? User { get; set; }
    public RsvpPerson? RsvpPerson { get; set; }
}

public enum PersonInviteStatus
{
    Pending,
    Invited,
    Responded,
    Declined,
    Cancelled
}
