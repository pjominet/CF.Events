namespace CF.Events.Web.Models;

public class EventConfig
{
    public int EventId { get; set; }

    public bool OfferDinner { get; set; }
    public bool OfferLunch { get; set; }
    public bool OfferBreakfast { get; set; }
    public bool OfferBrunch { get; set; }

    public bool ShowAccommodationOptions { get; set; }

    public bool AllowComments { get; set; } = true;
    public bool AllowPartners { get; set; } = true;
    public bool AllowKids { get; set; } = true;

    // navigation properties
    public Event? Event { get; set; }
}
