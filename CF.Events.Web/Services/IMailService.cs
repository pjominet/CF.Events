using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services;

public interface IMailService
{
    public Task SendInvitationAsync(InvitationEmailRequest request, CancellationToken ctx = default);

    public Task SendSaveTheDateAsync(SaveDateEmailRequest request, CancellationToken ctx = default);
    public Task SendSaveTheDateWithLinkAsync(SaveDateEmailRequest request, CancellationToken ctx = default);
}
