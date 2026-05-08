using System.ComponentModel.DataAnnotations;

namespace CF.Events.Shared.Models;

public class Rsvp
{
    public int Id { get; init; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool Attending { get; set; } = true;
    public bool BringsPlusOne { get; set; }
    public bool JoinForDinner { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }

    [Required]
    [StringLength(6)]
    public string AccessCode { get; set; } = "NONE";

    public DateTime SubmittedAt { get; init; } = DateTime.UtcNow;
}
