using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// Represents a day within a multi-day event.
/// Allows for per-day configuration of food and accommodation options.
/// </summary>
public class EventDay
{
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty; // ex: "Day 1", "Wedding Day", "Conference Day", etc.

    public bool OffersFood { get; set; } = true;
    public bool OffersAccommodation { get; set; } = true;

    // Navigation properties
    public Event Event { get; set; } = null!;
    public HashSet<RsvpFoodPreference> FoodPreferences { get; set; } = [];
    public HashSet<RsvpAccommodation> Accommodations { get; set; } = [];
}
