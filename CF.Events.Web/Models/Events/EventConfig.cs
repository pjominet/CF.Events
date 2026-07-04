using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// Configuration options for an event.
/// </summary>
public class EventConfig
{
    public int EventId { get; set; }

    // Accommodation settings
    public bool ShowAccommodationOptions { get; set; }

    public List<string> AccommodationLinks { get; set; } = []; // URL to external reservation pages

    [StringLength(100)]
    public string? AccommodationCode { get; set; }

    [StringLength(1000)]
    public string? AccommodationInfo { get; set; }


    // RSVP options
    public bool AllowComments { get; set; } = true;
    public bool AllowKids { get; set; } = true;

    // navigation properties
    public Event? Event { get; set; }
    public HashSet<CustomQuestion> CustomQuestions { get; set; } = [];
}
