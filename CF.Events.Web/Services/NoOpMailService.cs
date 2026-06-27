namespace CF.Events.Web.Services;

public class NoOpMailService(ILogger<NoOpMailService> logger) : IMailService
{
    public Task SendInvitationAsync(string eventName, string displayName, string email, string invitationCode, string? customDesign = null)
    {
        logger.LogDebug("Fake invitation sent: {EventName} {DisplayName} {Email} {InvitationCode} {CustomDesign}", eventName, displayName, email, invitationCode, customDesign);
        return Task.CompletedTask;
    }
}
