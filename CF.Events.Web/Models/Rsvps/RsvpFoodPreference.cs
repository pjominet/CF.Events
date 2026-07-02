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

    public DietaryOptions DietaryOption { get; set; } = DietaryOptions.None;

    [StringLength(500)]
    public string? SpecialRequests { get; set; }

    // Navigation properties
    public RsvpPerson RsvpPerson { get; set; } = null!;
    public EventDay EventDay { get; set; } = null!;
}
