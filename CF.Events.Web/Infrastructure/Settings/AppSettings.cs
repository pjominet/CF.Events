namespace CF.Events.Web.Infrastructure.Settings;

public class AppSettings
{
    public required string Name { get; init; }
    public required string TransactionalEmailApiKey { get; init; }
}
