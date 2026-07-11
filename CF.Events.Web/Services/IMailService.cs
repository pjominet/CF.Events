using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services;

public interface IMailService
{
    public Task SendInvitationAsync(InvitationEmailRequest request, CancellationToken ctx = default);

    public Task SendSaveTheDateAsync(SaveTheDateEmailRequest request, CancellationToken ctx = default);
}
