using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class Rsvp
{
    [Required]
    public int EventId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public bool Attending { get; set; } = true;
    public bool BringsPlusOne { get; set; }
    public bool JoinForDinner { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // navigation properties
    public UserEvent UserEvent { get; init; } = null!;
}
