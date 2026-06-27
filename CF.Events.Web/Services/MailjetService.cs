using CF.Events.Web.Models;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;

namespace CF.Events.Web.Services;

public class MailjetService(IMailjetClient mailjetClient)
{
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

    public async Task SendConfirmationLinkAsync(string displayName, string email, string confirmationLink)
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
                    { "TemplateID", 8135949 },
                    {
                        "Variables", new JObject
                        {
                            { "sender_sig", "Patrick & Éadaoin" },
                            { "confirmation_link", confirmationLink },
                            { "display_name", displayName }
                        }
                    }
                }
            });

        await SendMailjetEmailAsync(request);
    }

    public async Task SendPasswordResetLinkAsync(string displayName, string email, string resetLink)
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
                    { "TemplateID", 8136026 },
                    {
                        "Variables", new JObject
                        {
                            { "sender_sig", "Patrick & Éadaoin" },
                            { "reset_link", resetLink },
                            { "display_name", displayName }
                        }
                    }
                }
            });

        await SendMailjetEmailAsync(request);
    }

    public async Task SendPasswordResetCodeAsync(string displayName, string email, string resetCode)
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
                            { "reset_code", resetCode },
                            { "display_name", displayName }
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
