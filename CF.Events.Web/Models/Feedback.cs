using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class Feedback
{
    public int Id { get; set; }

    [StringLength(1000)]
    public required string Text { get; set; }

    [StringLength(450)]
    public required string UserId { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // navigation properties
    public AppUser User { get; set; } = null!;
}
