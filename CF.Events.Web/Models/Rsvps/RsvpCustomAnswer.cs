using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// Represents an answer to a custom question for a specific RSVP.
/// </summary>
public class RsvpCustomAnswer
{
    public int Id { get; set; }

    [Required]
    public int RsvpId { get; set; }

    [Required]
    public int CustomQuestionId { get; set; }

    // Store answer based on type
    [StringLength(1000)]
    public string? TextValue { get; set; }

    public bool? BooleanValue { get; set; }

    public int? NumberValue { get; set; }

    public DateTime? DateValue { get; set; }

    // For MultiChoice
    public List<string>? SelectedOptions { get; set; }

    // Navigation properties
    public Rsvp Rsvp { get; set; } = null!;
    public CustomQuestion Question { get; set; } = null!;
}
