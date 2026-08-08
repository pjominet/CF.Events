namespace CF.Events.Web.Models;

public class AccommodationDetails
{
    public bool HasRsvped { get; set; }
    public bool IsAttending { get; set; }
    public string? Details { get; set; }
    public string? Code { get; set; }
    public Dictionary<LinkType, string> BookingLinks { get; set; } = [];
}
