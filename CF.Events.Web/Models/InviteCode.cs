using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class InviteCode
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string Value { get; init; }

    [StringLength(450)]
    public required string UserId { get; init; }

    public int EventId { get; init; }

    public DateTime ValidUntil { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // navigation properties
    public AppUser User { get; init; } = null!;
    public Event Event { get; init; } = null!;
}
