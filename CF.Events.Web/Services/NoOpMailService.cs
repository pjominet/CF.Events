namespace CF.Events.Web.Services;

public class NoOpMailService(ILogger<NoOpMailService> logger) : IMailService
{
    public Task SendInvitationAsync(string eventName, string displayName, string email, string callBackUrl, string? customDesign = null, CancellationToken ctx = default)
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
            eventName, displayName, email, callBackUrl, string.IsNullOrEmpty(customDesign) ? "None" : "Yes");
        return Task.CompletedTask;
    }

    public Task SendSaveTheDateAsync(string templateId, string eventName, string displayName, string email, string returnUrl, CancellationToken ctx = default)
    {
        logger.LogDebug(
            """
            Fake Save the Date sent:
                Template ID: {TemplateId}
                Event: {EventName}
                Display Name: {DisplayName}
                Email: {Email},
                Return URL: {ReturnUrl}
            """,
            templateId, eventName, displayName, email, returnUrl);
        return Task.CompletedTask;
    }
}
