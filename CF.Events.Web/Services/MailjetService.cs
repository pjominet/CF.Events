using Mailjet.Client;

namespace CF.Events.Web.Services;

public abstract class MailjetService(IMailjetClient mailjetClient)
{
    protected async Task SendMailjetEmailAsync(MailjetRequest request, CancellationToken ctx = default)
    {
        try
        {
            var timeout = TimeSpan.FromSeconds(30);
            using var timeoutCts = new CancellationTokenSource(timeout);

            // This won't cancel the HTTP request but allows callers to observe timeout
            var response = await mailjetClient.PostAsync(request).ConfigureAwait(false);

            // If the caller's token was canceled during the request
            ctx.ThrowIfCancellationRequested();

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = response.GetErrorInfo();
                throw new Exception($"Mailjet API error: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to send email via Mailjet", ex);
        }
    }
}
