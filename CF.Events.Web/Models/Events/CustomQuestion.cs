using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// Represents a custom question that can be added to an event's RSVP form.
/// Used for event-specific information gathering beyond the structured fields.
/// </summary>
public class CustomQuestion
{
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    [StringLength(200)]
    public string Label { get; set; } = string.Empty;

    [StringLength(500)]
    public string? HelpText { get; set; }

    public CustomQuestionType Type { get; set; }

    // For choice types
    public List<string>? Options { get; set; }

    public bool IsRequired { get; set; }

    public int SortOrder { get; set; }

    // For stepper grouping
    public FormStep FormStep { get; set; } = FormStep.Extras;

    public int StepOrder { get; set; }

    // Navigation properties
    public EventConfig EventConfig { get; set; } = null!;
    public HashSet<RsvpCustomAnswer> Answers { get; set; } = [];
}

public enum CustomQuestionType
{
    Text,
    TextArea,
    Boolean,
    SingleChoice,
    MultiChoice,
    Number,
    Date
}

public enum FormStep
{
    Attendance,
    Kids,
    Food,
    Accommodation,
    Extras
}
