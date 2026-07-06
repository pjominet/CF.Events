using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class BookingLink
{
    public int Id { get; set; }

    public int EventId { get; set; }

    [StringLength(500)]
    public required string Link { get; set; }
    public LinkType Type { get; set; }

    // navigation properties
    public Event Event { get; set; } = null!;
}

public enum LinkType
{
    Email,
    Phone,
    Web
}
