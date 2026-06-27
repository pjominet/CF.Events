using Mailjet.Client;

namespace CF.Events.Web.Services;

public abstract class MailjetService(IMailjetClient mailjetClient)
{
    protected async Task SendMailjetEmailAsync(MailjetRequest request)
    {
        try
        {
            var response = await mailjetClient.PostAsync(request);
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
