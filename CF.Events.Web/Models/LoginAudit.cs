namespace CF.Events.Web.Models;

public class LoginAudit
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    public DateTime LoginAt { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? AuthMethod { get; set; }

    // navigation properties
    public AppUser User { get; set; } = null!;
}
