namespace CF.Events.Web.Models;

public class RsvpDetail
{
    public bool HasRsvped { get; set; }
    public bool IsAttending { get; set; }
    public required string EventName { get; set; }
    public List<ParticipantAttendance> ParticipantsAttendance { get; set; } = [];
    public string? AccommodationDetails { get; set; }
    public string? AccommodationCode { get; set; }
    public List<ParticipantDiet> ParticipantsDiets { get; set; } = [];
    public Dictionary<LinkType, string> BookingLinks { get; set; } = [];
    public string? DonationIban { get; set; }
    public string? DonationLink { get; set; }
    public string? DonationReference { get; set; }
}
