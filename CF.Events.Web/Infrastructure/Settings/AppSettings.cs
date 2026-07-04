namespace CF.Events.Web.Infrastructure.Settings;

public class AppSettings
{
    public required string Title { get; set; }
    public required int PasswordLength { get; init; }
    public required MailjetSettings Mailjet { get; init; }
    public string? BaseUrl { get; init; }
    public int? EmailBatchSize { get; init; }
    public int? EmailBatchIntervalHours { get; init; }
}

public class MailjetSettings
{
    public required string ApiKey { get; init; }
    public required string ApiSecret { get; init; }
}
