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

    [StringLength(500)]
    public string? AccommodationLink { get; set; } // URL to external reservation pages

    [StringLength(1000)]
    public string? AccommodationInfo { get; set; } // Additional text/instructions about accommodation

    // RSVP options
    public bool AllowComments { get; set; } = true;
    public bool AllowKids { get; set; } = true;

    // navigation properties
    public Event? Event { get; set; }
}
