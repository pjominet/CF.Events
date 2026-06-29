using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class Event
{
    public int Id { get; init; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    [StringLength(255)]
    public string? InvitationFileName { get; set; }

    [StringLength(255)]
    public string? OriginalInvitationFileName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // navigation properties
    public HashSet<UserEvent> EventUsers { get; set; } = [];
    public HashSet<InviteCode> InviteCodes { get; set; } = [];
    public EventConfig? EventConfig { get; set; }
}
