namespace CF.Events.Web.Models;

public class EventScheduleViewModel
{
    public DateTime EventStartDate { get; set; }
    public List<EventScheduleStep> Steps { get; set; } = [];
}
