using CF.Events.Web.Models;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Newtonsoft.Json.Linq;

namespace CF.Events.Web.Services;

public class MailjetEmailSender(IMailjetClient mailjetClient) : MailjetService(mailjetClient), IEmailSender<AppUser>
{
    public async Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
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
                                { "Name", user.DisplayName }
                            }
                        }
                    },
                    { "TemplateID", 8135949 },
                    {
                        "Variables", new JObject
                        {
                            { "sender_sig", "Patrick & Éadaoin" },
                            { "confirmation_link", confirmationLink },
                            { "display_name", user.DisplayName }
                        }
                    }
                }
            });

        await SendMailjetEmailAsync(request);
    }

    public async Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
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
                                { "Name", user.DisplayName }
                            }
                        }
                    },
                    { "TemplateID", 8136026 },
                    {
                        "Variables", new JObject
                        {
                            { "sender_sig", "Patrick & Éadaoin" },
                            { "reset_link", resetLink },
                            { "display_name", user.DisplayName }
                        }
                    }
                }
            });

        await SendMailjetEmailAsync(request);
    }

    public async Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
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
                                { "Name", user.DisplayName }
                            }
                        }
                    },
                    { "TemplateID", 8136101 },
                    {
                        "Variables", new JObject
                        {
                            { "sender_sig", "Patrick & Éadaoin" },
                            { "reset_code", resetCode },
                            { "display_name", user.DisplayName }
                        }
                    }
                }
            });

        await SendMailjetEmailAsync(request);
    }
}
