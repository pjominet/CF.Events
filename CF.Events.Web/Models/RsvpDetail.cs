namespace CF.Events.Web.Models;

public class RsvpDetail
{
    public bool HasRsvped { get; set; }
    public bool IsAttending { get; set; }
    public required string EventName { get; set; }
    public string? AccommodationDetails { get; set; }
    public string? AccommodationCode { get; set; }
    public Dictionary<LinkType, string> BookingLinks { get; set; } = [];
    public string? DonationIban { get; set; }
    public string? DonationReference { get; set; }
    public List<int> AttendanceDays { get; set; } = [];
}
