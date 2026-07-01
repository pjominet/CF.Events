using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class InviteCode
{
    public int Id { get; init; }

    [Required]
    [StringLength(100)]
    public required string Code { get; init; }

    [StringLength(100)]
    public string? Label { get; init; }

    public int EventId { get; init; }

    public DateTime ValidUntil { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // navigation properties
    public Event Event { get; init; } = null!;
    public HashSet<Invitation> Invitations { get; set; } = [];
}
