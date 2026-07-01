namespace CF.Events.Web.Models;

public class EventConfig
{
    public int EventId { get; set; }

    public bool ShowFoodOptions { get; set; }

    public bool ShowAccommodationOptions { get; set; }

    public bool AllowComments { get; set; } = true;
    public bool AllowPartners { get; set; } = true;
    public bool AllowKids { get; set; } = true;

    // navigation properties
    public Event? Event { get; set; }
}
