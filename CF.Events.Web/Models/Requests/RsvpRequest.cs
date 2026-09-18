using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models.Requests;

public class RsvpRequest
{
    public List<string> Participants { get; set; } = [];
    public bool Attending { get; set; } = true;
    public List<ParticipantAttendance> ParticipantsAttendance { get; set; } = [];
    public List<ParticipantDiet> ParticipantsDiets { get; set; } = [];
    [StringLength(500)]
    public string? Comments { get; set; }
}
