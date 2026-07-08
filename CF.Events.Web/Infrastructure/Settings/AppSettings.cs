namespace CF.Events.Web.Infrastructure.Settings;

public class AppSettings
{
    public required string Title { get; set; }
    public required int PasswordLength { get; init; }
    public required EmailProviderSettings EmailProviderSettings { get; init; }
    public string? BaseUrl { get; init; }
    public int? EmailBatchSize { get; init; }
    public int? EmailBatchIntervalHours { get; init; }
}

public class EmailProviderSettings
{
    public required string ApiKey { get; init; }
    public required string SenderEmail { get; init; }
    public required string SenderName { get; init; }
}
