namespace CF.Events.Web.Services;

public interface IMailService
{
    public Task SendInvitationAsync(string eventName, string displayName, string email, string callBackUrl, string? customDesign = null, CancellationToken ctx = default);
}
