namespace CF.Events.Web.Services;

public class NoOpMailService(ILogger<NoOpMailService> logger) : IMailService
{
    public Task SendInvitationAsync(string eventName, string displayName, string email, string callBackUrl, string? customDesign = null)
    {
        logger.LogDebug(
            """
            Fake invitation sent:
                Event: {EventName}
                Display Name: {DisplayName}
                Email: {Email}
                Callback URL: {CallBackUrl}
                Custom Design: {CustomDesign}
            """,
            eventName, displayName, email, callBackUrl, customDesign);
        return Task.CompletedTask;
    }
}
