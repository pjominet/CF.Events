namespace CF.Events.Web.Services;

public interface IEmailProvider
{
    Task SendTemplatedEmailAsync(string templateId, string to, IDictionary<string, string> variables, CancellationToken ctx = default);
}
