using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;

namespace CF.Events.Web.Services;

public class MailService(IMailjetClient mailjetClient) : MailjetService(mailjetClient), IMailService
{
    public async Task SendInvitationAsync(string eventName, string displayName, string email, string callBackUrl, string? customDesign = null, CancellationToken ctx = default)
    {
        var request = new MailjetRequest
            {
                Resource = Send.Resource
            }
            .Property(Send.Messages, new JArray
            {
                new JObject
                {
                    {
                        Send.To, new JArray
                        {
                            new JObject
                            {
                                { "Email", email },
                                { "Name", displayName }
                            }
                        }
                    },
                    { "TemplateID", 0 },
                    {
                        "Variables", new JObject
                        {
                            { "sender_sig", "Patrick & Éadaoin" },
                            { "invitation_callback", callBackUrl },
                            { "display_name", displayName },
                            { "event_name", eventName },
                            { "custom_design", customDesign }
                        }
                    }
                }
            });

        await SendMailjetEmailAsync(request, ctx);
    }

    public async Task SendSaveTheDateAsync(string templateId,string eventName, string displayName, string email, string returnUrl, CancellationToken ctx = default)
    {
        var request = new MailjetRequest
            {
                Resource = Send.Resource
            }
            .Property(Send.Messages, new JArray
            {
                new JObject
                {
                    {
                        Send.To, new JArray
                        {
                            new JObject
                            {
                                { "Email", email },
                                { "Name", displayName }
                            }
                        }
                    },
                    { "TemplateID", templateId },
                    {
                        "Variables", new JObject
                        {
                            { "sender_sig", "Patrick & Éadaoin" },
                            { "display_name", displayName },
                            { "event_name", eventName },
                            { "return_url", returnUrl }
                        }
                    }
                }
            });

        await SendMailjetEmailAsync(request, ctx);
    }
}
