using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class EventFaqItem
{
    public int Id { get; set; }
    public int EventId { get; set; }
    [StringLength(500)]
    public required string Question { get; set; }
    [StringLength(1000)]
    public required string Answer { get; set; }

    public int SortOrder { get; set; }
    // navigation properties
    public Event Event { get; set; } = null!;
}
