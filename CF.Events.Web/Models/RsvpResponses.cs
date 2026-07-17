namespace CF.Events.Web.Models;

public class RsvpResponses
{
    public required string GuestGroup { get; set; }
    public List<ParticipantAttendance> ParticipantsAttendance { get; init; } = [];
    public List<ParticipantDiet> ParticipantsDiets { get; init; } = [];
    public string? Comments { get; init; }
}
