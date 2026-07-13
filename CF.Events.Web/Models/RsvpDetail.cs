namespace CF.Events.Web.Models;

public class RsvpDetail
{
    public bool HasRsvped { get; set; }
    public bool IsAttending { get; set; }
    public required string EventName { get; set; }
    public Dictionary<int, int> AttendanceDays { get; set; } = [];
    public string? AccommodationDetails { get; set; }
    public string? AccommodationCode { get; set; }
    public int DietaryOptionNbrPeople { get; set; }
    public List<DietaryOptions> CommonDietaryOptions { get; set; } = [];
    public string? OtherDietaryDetails { get; set; }
    public Dictionary<LinkType, string> BookingLinks { get; set; } = [];
    public string? DonationIban { get; set; }
    public string? DonationReference { get; set; }
}
