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
    public string QuestionId { get; set; } = Guid.NewGuid().ToString(); // For easy reference

    [Required]
    [StringLength(200)]
    public string Label { get; set; } = string.Empty;

    [StringLength(500)]
    public string? HelpText { get; set; }

    public CustomQuestionType Type { get; set; }

    // For choice types
    public List<string>? Options { get; set; }

    public bool IsRequired { get; set; } = false;

    public int SortOrder { get; set; }

    // For stepper grouping
    [StringLength(50)]
    public string StepGroup { get; set; } = "Extras"; // "Attendance", "Food", "Accommodation", "Extras", "Custom"

    public int StepOrder { get; set; } // Order within the step

    // Conditional display
    [StringLength(100)]
    public string? ShowIf { get; set; } // Expression: "Attending == true", "Kids.Count > 0"

    // Navigation properties
    public Event Event { get; set; } = null!;
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
