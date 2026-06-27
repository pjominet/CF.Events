using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;

namespace CF.Events.Web.Services;

public class MailService(IMailjetClient mailjetClient) : MailjetService(mailjetClient), IMailService
{
    public async Task SendInvitationAsync(string eventName, string displayName, string email, string invitationCode, string? customDesign = null)
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
                    { "TemplateID", 8136101 },
                    {
                        "Variables", new JObject
                        {
                            { "sender_sig", "Patrick & Éadaoin" },
                            { "invitation_code", invitationCode },
                            { "display_name", displayName },
                            { "event_name", eventName },
                            { "custom_design", customDesign }
                        }
                    }
                }
            });

        await SendMailjetEmailAsync(request);
    }
}
