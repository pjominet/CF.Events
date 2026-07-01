using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// Represents a person's food preferences for a specific event day.
/// </summary>
public class RsvpFoodPreference
{
    public int Id { get; set; }

    [Required]
    public int RsvpPersonId { get; set; }

    [Required]
    public int EventDayId { get; set; }

    public bool JoinsForBreakfast { get; set; }
    public bool JoinsForLunch { get; set; }
    public bool JoinsForDinner { get; set; }
    public bool JoinsForBrunch { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; } // Special requests for this day

    // Navigation properties
    public RsvpPerson RsvpPerson { get; set; } = null!;
    public EventDay EventDay { get; set; } = null!;
}
