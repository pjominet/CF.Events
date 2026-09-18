using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services;

public interface IMailService
{
    public Task SendTemplatedEmailAsync(TemplateEmailRequest request, CancellationToken ctx = default);
    public Task SendTemplatedEmailsBulkAsync(IEnumerable<TemplateEmailRequest> requests, CancellationToken ctx = default);
}
