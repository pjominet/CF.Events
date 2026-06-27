namespace CF.Events.Web.Services;

public interface IMailService
{
    public Task SendInvitationAsync(string eventName, string displayName, string email, string invitationCode, string? customDesign = null);
}
