namespace CF.Events.Web.Services;

public interface IMailService
{
    public Task SendInvitationAsync(string eventName, string displayName, string email, string callBackUrl, string? customDesign = null, CancellationToken ctx = default);

    public Task SendSaveTheDateAsync(string templateId, string eventName, string displayName, string email, string returnUrl, CancellationToken ctx = default);
}
