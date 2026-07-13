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

    [StringLength(500)]
    public string? Comments { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // navigation properties
    public EventUser EventUser { get; init; } = null!;
    public List<ParticipantAttendance> ParticipantsAttendance { get; set; } = [];
    public List<ParticipantDiet> ParticipantsDiets { get; set; } = [];
}

public class ParticipantDiet
{
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [StringLength(500)]
    public required string ParticipantName { get; set; }
    public List<DietaryRestrictions> Restrictions { get; set; } = [];

    [StringLength(500)]
    public string? OtherDetails { get; set; }

    // Navigation property
    public Rsvp Rsvp { get; set; } = null!;
}

public class ParticipantAttendance
{
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [StringLength(500)]
    public required string ParticipantName { get; set; }
    public List<int> AttendingDays { get; set; } = [];

    // Navigation property
    public Rsvp Rsvp { get; set; } = null!;
}

public enum DietaryRestrictions
{
    None,
    Vegetarian,
    Vegan,
    Pescetarian,
    GlutenIntolerant,
    DairyIntolerant,
    LactoseIntolerant,
}
