namespace CF.Events.Web.Infrastructure.Settings;

public class AppSettings
{
    public required string Name { get; init; }
    public required MailjetSettings Mailjet { get; init; }
}

public class MailjetSettings
{
    public required string ApiKey { get; init; }
    public required string ApiSecret { get; init; }
    public required string SenderEmail { get; init; }
    public required string SenderName { get; init; }
}
