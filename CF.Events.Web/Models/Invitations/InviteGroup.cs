using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// Represents an individual person within a group invitation.
/// </summary>
public class InviteGroup
{
    public int Id { get; set; }

    [Required]
    public int InvitationId { get; set; }
    public int InviteCodeId { get; set; }

    [StringLength(450)]
    public required string PrimaryUserId { get; set; }

    [StringLength(450)]
    public string? SecondaryUserId { get; set; }

    [StringLength(255)]
    public string? PlusOneName { get; set; }

    [StringLength(100)]
    public string? AssignedAccommodationCode { get; set; }

    // Navigation properties
    public Invitation Invitation { get; set; } = null!;
    public InviteToken InviteToken { get; set; } = null!;
    public AppUser PrimaryUser { get; set; } = null!;
    public AppUser? SecondaryUser { get; set; }
}
