using System.ComponentModel.DataAnnotations;

namespace CF.Events.API.Models;

public class Rsvp
{
    public int Id { get; set; }

    [Required] [StringLength(100)] public string Name { get; set; } = string.Empty;

    public bool Attending { get; set; }
    public bool BringsPlusOne { get; set; }
    public bool JoinForDinner { get; set; }

    [StringLength(500)] public string? Comments { get; set; }

    [Required] [StringLength(255)] public string Fingerprint { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
