using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services.Interfaces;

public interface IEmailSender
{
    public Task SendTemplatedEmailAsync(TemplateEmailRequest request, CancellationToken ctx = default);
    public Task SendTemplatedEmailsAsync(IEnumerable<TemplateEmailRequest> requests, CancellationToken ctx = default);
}
