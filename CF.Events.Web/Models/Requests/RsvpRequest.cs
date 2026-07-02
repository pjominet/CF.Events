using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// DTO for submitting/completing an RSVP for an invitation.
/// This is the main request that contains all RSVP data across all steps.
/// </summary>
public class RsvpRequest
{
    [Required]
    public int InvitationId { get; set; }

    // Group/Attendance info (Step 1)
    [StringLength(100)]
    public string? GroupName { get; set; }

    public List<RsvpPersonRequest> People { get; set; } = [];

    // Kids info
    public Dictionary<KidAgeBracket, int>? KidsDetails { get; set; }

    // Food preferences per person per day (Step 3)
    public List<RsvpFoodPreferenceRequest> FoodPreferences { get; set; } = [];

    // Accommodation per person per day (Step 4)
    public List<RsvpAccommodationRequest> Accommodations { get; set; } = [];

    // Custom question answers (Step 5)
    public List<RsvpCustomAnswerRequest> CustomAnswers { get; set; } = [];

    // General comments (Step 6)
    [StringLength(500)]
    public string? Comments { get; set; }

    // Submission metadata
    public bool IsDraft { get; set; } = true;
}

/// <summary>
/// DTO for a person's RSVP data.
/// </summary>
public class RsvpPersonRequest
{
    public int? Id { get; set; } // Null for new persons (plus ones)
    public int? InvitedPersonId { get; set; } // Null for ad-hoc plus ones

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Email { get; set; }

    public bool IsPlusOne { get; set; } = false;
    public bool IsPrimary { get; set; } = false;

    [Required]
    public bool Attending { get; set; } = true;

}

/// <summary>
/// DTO for food preferences for a specific person and day.
/// </summary>
public class RsvpFoodPreferenceRequest
{
    public int? Id { get; set; }
    [Required]
    public int RsvpPersonId { get; set; }
    [Required]
    public int EventDayId { get; set; }

    public DietaryOptions DietaryOption { get; set; } = DietaryOptions.None;

    [StringLength(500)]
    public string? SpecialRequests { get; set; }
}

/// <summary>
/// DTO for accommodation needs for a specific person and day.
/// </summary>
public class RsvpAccommodationRequest
{
    public int? Id { get; set; }
    public int RsvpPersonId { get; set; }
    public int EventDayId { get; set; }

    public bool HasBooked { get; set; }
}

/// <summary>
/// DTO for custom question answer.
/// </summary>
public class RsvpCustomAnswerRequest
{
    public int? Id { get; set; }
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
}
