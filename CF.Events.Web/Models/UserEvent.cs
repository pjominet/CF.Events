namespace CF.Events.Web.Models;

public class UserEvent
{
    public required string UserId { get; set; }
    public required int EventId { get; set; }

    public Event Event { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
