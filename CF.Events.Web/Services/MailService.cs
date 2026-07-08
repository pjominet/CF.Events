using CF.Events.Web.Infrastructure.Settings;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace CF.Events.Web.Services;

public class MailService(IMailjetClient mailjetClient, IOptions<AppSettings> settings) : MailjetService(mailjetClient), IMailService
{
    private readonly MailjetSettings _mailjetSettings = settings.Value.Mailjet;

    public async Task SendInvitationAsync(string eventName, string displayName, string email, string callBackUrl, string? customDesign = null, CancellationToken ctx = default)
    {
        var request = new MailjetRequest
            {
                Resource = Send.Resource
            }
            .Property(Send.FromEmail, _mailjetSettings.SenderEmail)
            .Property(Send.FromName, _mailjetSettings.SenderName)
            .Property(Send.Recipients, new JArray
            {
                new JObject
                {
                    { "Email", email },
                    { "Name", displayName }
                }
            })
            .Property("Mj-TemplateID", 0)
            .Property("Mj-TemplateLanguage", true)
            .Property("Variables", new JObject
            {
                { "sender_sig", "Patrick & Éadaoin" },
                { "invitation_callback", callBackUrl },
                { "display_name", displayName },
                { "event_name", eventName },
                { "custom_design", customDesign }
            });

        await SendMailjetEmailAsync(request, ctx);
    }

    public async Task SendSaveTheDateAsync(string templateId,string eventName, string displayName, string email, string returnUrl, CancellationToken ctx = default)
    {
        var request = new MailjetRequest
            {
                Resource = Send.Resource
            }
            .Property(Send.FromEmail, _mailjetSettings.SenderEmail)
            .Property(Send.FromName, _mailjetSettings.SenderName)
            .Property(Send.Recipients, new JArray
            {
                new JObject
                {
                    { "Email", email },
                    { "Name", displayName }
                }
            })
            .Property("Mj-TemplateID", templateId)
            .Property("Mj-TemplateLanguage", true)
            .Property("Variables", new JObject
            {
                { "sender_sig", "Patrick & Éadaoin" },
                { "display_name", displayName },
                { "event_name", eventName },
                { "return_url", returnUrl }
            });

        await SendMailjetEmailAsync(request, ctx);
    }
}
