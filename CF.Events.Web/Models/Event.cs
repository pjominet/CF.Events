using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CF.Events.Web.Models;

public class Event
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    [Required]
    public string Description { get; set; } = null!;
    public string? TravelInstructions { get; set; }

    public List<string> AccommodationCodes { get; set; } = [];

    [StringLength(500)]
    public string? AccommodationDetails { get; set; }

    [StringLength(64)]
    public string? DonationIban { get; set; }

    [StringLength(1000)]
    public string? DonationLink { get; set; }

    [StringLength(255)]
    public string? SaveDateTemplateId { get; set; }

    public int InviteValidity { get; set; } = 30;
    public int MaxParticipantsPerRsvp { get; set; } = 4;

    [StringLength(255)]
    public string? InvitationTemplateId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // navigation properties
    public List<EventUser> EventUsers { get; set; } = [];
    public List<BookingLink> BookingLinks { get; set; } = [];
    public List<EventFaqItem> EventFaq { get; set; } = [];
    public List<EventScheduleStep> EventSchedule { get; set; } = [];
    public List<EventImage> EventImages { get; set; } = [];

    // helper
    [NotMapped]
    public int EventDuration
    {
        get
        {
            var duration = (int)Math.Round((EndDate - StartDate).TotalDays);
            // Return the number of days + 1 to include both start and end dates as part of the duration
            return Math.Max(1, duration + 1);
        }
    }
}
