using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class Feedback
{
    public int Id { get; set; }

    [Required]
    [StringLength(1000)]
    public string Text { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // navigation properties
    public AppUser User { get; set; } = null!;
}
