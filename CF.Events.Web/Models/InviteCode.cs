using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class InviteCode
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string Code { get; init; }

    public int EventId { get; init; }

    public DateTime ValidUntil { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public Event? Event { get; init; }
}
