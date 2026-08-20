namespace CF.Events.Web.Infrastructure.Settings;

public class AppSettings
{
    public required string Title { get; set; }
    public required int PasswordLength { get; init; }
    public int GuestLoginValidityMonths { get; set; }
    public required EmailProviderSettings EmailProviderSettings { get; init; }
    public required string BaseUrl { get; init; }
    public int? EmailBatchSize { get; init; }
    public int? EmailBatchIntervalHours { get; init; }
}

public class EmailProviderSettings
{
    public required string SenderEmail { get; init; }
    public required string SenderName { get; init; }
}
