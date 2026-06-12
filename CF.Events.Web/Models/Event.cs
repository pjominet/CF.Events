using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class Event
{
    public int Id { get; init; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Type { get; set; } = "Wedding"; // e.g., Wedding, Engagement

    public DateTime Date { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public string? Location { get; set; }

    public string? InvitationFileName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
