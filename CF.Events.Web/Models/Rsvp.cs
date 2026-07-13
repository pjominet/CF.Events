using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class Rsvp
{
    [Required]
    public int EventId { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    public bool Attending { get; set; } = true;

    public Dictionary<int, int> AttendanceDays { get; set; } = [];
    public List<DietaryOptions> CommonDietaryOptions { get; set; } = [];

    [StringLength(500)]
    public string? OtherDietaryDetails { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // navigation properties
    public EventUser EventUser { get; init; } = null!;
}

public enum DietaryOptions
{
    None,
    Vegetarian,
    Vegan,
    Pescetarian,
    GlutenIntolerant,
    DairyIntolerant,
    LactoseIntolerant,
}
