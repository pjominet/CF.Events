using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace CF.Events.Web.Services;

public class MailjetEmailSender(IMailjetClient mailjetClient) : IEmailSender<ApplicationUser>, IEmailSender
{
    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
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
                            { "display_name", user.DisplayName ?? user.UserName ?? "undefined" }
                        }
                    }
                }
            });

        await SendMailjetEmailAsync(request);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
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
                            { "display_name", user.DisplayName ?? user.UserName ?? "undefined" }
                        }
                    }
                }
            });

        await SendMailjetEmailAsync(request);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
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
                            { "display_name", user.DisplayName ?? user.UserName ?? "undefined" }
                        }
                    }
                }
            });

        await SendMailjetEmailAsync(request);
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var request = new MailjetRequest
            {
                Resource = Send.Resource
            }
            .Property(Send.FromEmail, "patrick@jominet.tech")
            .Property(Send.FromName, "Patrick & Éadaoin")
            .Property(Send.Subject, subject)
            .Property(Send.HtmlPart, htmlMessage)
            .Property(Send.Recipients, new JArray {
                new JObject {
                    {"Email", email}
                }
            });

        await SendMailjetEmailAsync(request);
    }

    private async Task SendMailjetEmailAsync(MailjetRequest request)
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
