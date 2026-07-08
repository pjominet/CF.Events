namespace CF.Events.Web.Services;

public class MailService(IEmailProvider emailProvider) : IMailService
{
    public async Task SendInvitationAsync(string eventName, string displayName, string email, string callBackUrl, string? customDesign = null, CancellationToken ctx = default)
    {
        var variables = new Dictionary<string, string>
        {
            { "sender_sig", "Patrick & Éadaoin" },
            { "invitation_callback", callBackUrl },
            { "display_name", displayName },
            { "event_name", eventName }
        };

        if (customDesign != null)
        {
            variables.Add("custom_design", customDesign);
        }

        await emailProvider.SendTemplatedEmailAsync("0", email, variables, ctx);
    }

    public async Task SendSaveTheDateAsync(string templateId, string eventName, string displayName, string email, string returnUrl, CancellationToken ctx = default)
    {
        var variables = new Dictionary<string, string>
        {
            { "sender_sig", "Patrick & Éadaoin" },
            { "display_name", displayName },
            { "event_name", eventName },
            { "return_url", returnUrl }
        };

        await emailProvider.SendTemplatedEmailAsync(templateId, email, variables, ctx);
    }
}
