using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

/// <summary>
/// Represents a person's accommodation acknowledgement for a specific event day/night.
/// The actual booking is done externally via reservation links in EventConfig.
/// </summary>
public class RsvpAccommodation
{
    public int Id { get; set; }

    [Required]
    public int RsvpPersonId { get; set; }

    [Required]
    public int EventDayId { get; set; }

    public bool HasBooked { get; set; } // Whether the guest has booked their accommodation

    // Navigation properties
    public RsvpPerson RsvpPerson { get; set; } = null!;
    public EventDay EventDay { get; set; } = null!;
}
