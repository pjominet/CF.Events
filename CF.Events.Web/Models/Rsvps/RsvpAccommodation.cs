using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// Represents a person's accommodation needs for a specific event day/night.
/// </summary>
public class RsvpAccommodation
{
    public int Id { get; set; }

    [Required]
    public int RsvpPersonId { get; set; }

    [Required]
    public int EventDayId { get; set; } // Which night (stays the night of this day)

    public bool NeedsAccommodation { get; set; }

    [StringLength(100)]
    public string? RoomType { get; set; } // Single, Double, Family, etc.

    [StringLength(500)]
    public string? SpecialRequests { get; set; }

    // Navigation properties
    public RsvpPerson RsvpPerson { get; set; } = null!;
    public EventDay EventDay { get; set; } = null!;
}
