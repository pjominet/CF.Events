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
    public bool BringsPlusOne { get; set; }
    public bool BringsKids { get; set; }
    // number of kids per age bracket
    public Dictionary<KidAgeBracket, int> KidsDetails { get; set; } = [];

    public bool JoinsForDinner { get; set; }
    public bool JoinsForLunch { get; set; }
    public DietaryOptions[] CommonDietaryOptions { get; set; } = [];
    [StringLength(500)]
    public string? OtherDietaryDetails { get; set; }

    public bool NeedsAccommodation { get; set; }
    [StringLength(100)]
    public string? AccommodationCode { get; set; }
    public int AccommodationDuration { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // navigation properties
    public UserEvent UserEvent { get; init; } = null!;
}

public enum DietaryOptions
{
    Vegetarian,
    Vegan,
    Pescetarian,
    GlutenIntolerant,
    DairyIntolerant,
    LactoseIntolerant,
}

public enum KidAgeBracket
{
    ZeroToThree,
    FourToEight,
    NineToFifteen,
    SixteenOrOlder
}
