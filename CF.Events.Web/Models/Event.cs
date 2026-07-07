using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CF.Events.Web.Models;

public class Event
{
    public int Id { get; init; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    public List<string> AccommodationCodes { get; set; } = [];

    [StringLength(1000)]
    public string? AccommodationDetails { get; set; }

    [StringLength(1000)]
    public string? DonationIban { get; set; }

    [StringLength(255)]
    public string? InvitationFileName { get; set; }

    [StringLength(255)]
    public string? OriginalInvitationFileName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // navigation properties
    public List<EventUser> EventUsers { get; set; } = [];
    public List<InviteCode> InviteCodes { get; set; } = [];
    public List<BookingLink> BookingLinks { get; set; } = [];

    // helper
    [NotMapped]
    public int EventDuration
    {
        get
        {
            // Calculate the difference in days.
            var duration = (int)Math.Round((EndDate - StartDate).TotalDays);
            // Return the number of days + 1 to include both start and end dates as part of the duration.
            return Math.Max(1, duration + 1);
        }
    }
}
