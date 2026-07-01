namespace CF.Events.Web.Models;

/// <summary>
/// Response DTO containing all data needed to render the RSVP stepper form.
/// </summary>
public class RsvpFormResponse
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
    public DateTime EventStartDate { get; set; }
    public DateTime EventEndDate { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }

    // Group info
    public int InvitationId { get; set; }
    public string? InvitationGroupName { get; set; }
    public string? AssignedAccommodationCode { get; set; }

    // Event configuration
    public bool ShowAccommodationOptions { get; set; }
    public string? AccommodationLink { get; set; }
    public string? AccommodationInfo { get; set; }
    public bool AllowComments { get; set; } = true;
    public bool AllowKids { get; set; } = true;

    // Event days
    public List<EventDayResponse> EventDays { get; set; } = [];

    // Invited persons (for pre-populating the form)
    public List<InvitedPersonResponse> InvitedPersons { get; set; } = [];

    // Custom questions
    public List<CustomQuestionResponse> CustomQuestions { get; set; } = [];

    // Existing RSVP data (if editing)
    public ExistingRsvpData? ExistingRsvp { get; set; }
}

/// <summary>
/// Simplified event day data for the RSVP form.
/// </summary>
public class EventDayResponse
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool OffersFood { get; set; } = true;
    public bool OffersAccommodation { get; set; } = true;
}

/// <summary>
/// Simplified invited person data for the RSVP form.
/// </summary>
public class InvitedPersonResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsUser { get; set; } // Whether this person is the logged-in user
}

/// <summary>
/// Custom question data for the RSVP form.
/// </summary>
public class CustomQuestionResponse
{
    public int Id { get; set; }
    public string QuestionId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public CustomQuestionType Type { get; set; }
    public List<string>? Options { get; set; }
    public bool IsRequired { get; set; }
    public string StepGroup { get; set; } = "Extras";
    public int StepOrder { get; set; }
    public string? ShowIf { get; set; }
    public string? PreviousAnswer { get; set; } // For editing existing RSVPs
}

/// <summary>
/// Existing RSVP data for editing.
/// </summary>
public class ExistingRsvpData
{
    public int RsvpId { get; set; }
    public string? GroupName { get; set; }
    public RsvpStatus Status { get; set; }
    public string? Comments { get; set; }
    public Dictionary<KidAgeBracket, int>? KidsDetails { get; set; }
    public List<ExistingRsvpPersonData> People { get; set; } = [];
    public List<ExistingRsvpFoodPreferenceData> FoodPreferences { get; set; } = [];
    public List<ExistingRsvpAccommodationData> Accommodations { get; set; } = [];
    public List<ExistingRsvpCustomAnswerData> CustomAnswers { get; set; } = [];
}

public class ExistingRsvpPersonData
{
    public int Id { get; set; }
    public int? InvitedPersonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsPlusOne { get; set; }
    public bool IsPrimary { get; set; }
    public bool Attending { get; set; }
    public DietaryOptions[]? DietaryRestrictions { get; set; }
    public string? OtherDietaryDetails { get; set; }
}

public class ExistingRsvpFoodPreferenceData
{
    public int Id { get; set; }
    public int RsvpPersonId { get; set; }
    public int EventDayId { get; set; }
    public bool JoinsForBreakfast { get; set; }
    public bool JoinsForLunch { get; set; }
    public bool JoinsForDinner { get; set; }
    public bool JoinsForBrunch { get; set; }
    public string? Notes { get; set; }
}

public class ExistingRsvpAccommodationData
{
    public int Id { get; set; }
    public int RsvpPersonId { get; set; }
    public int EventDayId { get; set; }
    public bool NeedsAccommodation { get; set; }
    public string? RoomType { get; set; }
    public string? SpecialRequests { get; set; }
}

public class ExistingRsvpCustomAnswerData
{
    public int Id { get; set; }
    public int CustomQuestionId { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }
    public int? NumberValue { get; set; }
    public DateTime? DateValue { get; set; }
    public List<string>? SelectedOptions { get; set; }
}

/// <summary>
/// Simple response for RSVP submission.
/// </summary>
public class RsvpSubmissionResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? RsvpId { get; set; }
    public RsvpStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public List<string> Errors { get; set; } = [];
}
