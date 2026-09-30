namespace CF.Events.Web.Models;

public class EmailActivity
{
    public required string EmailId { get; set; }

    public required string FromEmail { get; set; }

    public required string RecipientEmail { get; set; }

    public string? Subject { get; set; }

    public DateTime SentAt { get; set; }

    public bool IsDelivered { get; set; }

    public bool IsBounced { get; set; }

    public bool IsSpam { get; set; }

    public bool WasOpened { get; set; }

    public int OpenCount { get; set; }

    public DateTime? FirstOpenedAt { get; set; }

    public DateTime? LastOpenedAt { get; set; }

    public bool WasClicked { get; set; }

    public int ClickCount { get; set; }

    public DateTime? FirstClickedAt { get; set; }

    public DateTime? LastClickedAt { get; set; }

    public bool HasError { get; set; }

    public bool IsSandboxed { get; set; }

    public string? LastErrorMessage { get; set; }

    public string? LastSmtpResponse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Timeline navigation property
    public List<EmailActivityEvent> TimelineEvents { get; set; } = [];
}
