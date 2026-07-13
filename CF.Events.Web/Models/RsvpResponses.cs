namespace CF.Events.Web.Models;

public class RsvpResponses
{
    public Dictionary<int, int> AttendanceDays { get; init; } = [];
    public List<DietaryOptions> DietaryOptions { get; init; } = [];
    public string? OtherDietaryDetails { get; init; }
    public string? Comments { get; init; }
}
