namespace CF.Events.Web.Models;

public class EmailActivityEvent
{
    public int EmailActivityId { get; set; }

    public required string EmailId { get; set; }

    public required string Event { get; set; }

    public DateTime EventAt { get; set; }

    public string? SmtpResponse { get; set; }

    public string? ErrorMessage { get; set; }

    public string? Host { get; set; }

    public string? UserAgent { get; set; }

    public string? ClickUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public EmailActivity EmailActivity { get; set; } = null!;
}
