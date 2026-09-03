using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class EventScheduleStep
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int Day { get; set; }
    public TimeOnly TimeStamp { get; set; }
    [StringLength(500)]
    public required string Label { get; set; }

    // navigation properties
    public Event Event { get; set; } = null!;
}
