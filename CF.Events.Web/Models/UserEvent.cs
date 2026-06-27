namespace CF.Events.Web.Models;

public class UserEvent
{
    public string UserId { get; set; }
    public int EventId { get; set; }

    public Event Event { get; set; }
    public ApplicationUser User { get; set; }
}
