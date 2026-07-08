using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace CF.Events.Web.Services;

public class MailjetEmailSender(IMailjetClient mailjetClient, IOptions<AppSettings> settings) : MailjetService(mailjetClient), IEmailSender<AppUser>
{
    private readonly MailjetSettings _mailjetSettings = settings.Value.Mailjet;

    public async Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
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
                    { "Name", user.DisplayName }
                }
            })
            .Property("Mj-TemplateID", 8135949)
            .Property("Mj-TemplateLanguage", true)
            .Property("Variables", new JObject
            {
                { "sender_sig", "Patrick & Éadaoin" },
                { "confirmation_link", confirmationLink },
                { "display_name", user.DisplayName }
            });

        await SendMailjetEmailAsync(request);
    }

    public async Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
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
                    { "Name", user.DisplayName }
                }
            })
            .Property("Mj-TemplateID", 8136026)
            .Property("Mj-TemplateLanguage", true)
            .Property("Variables", new JObject
            {
                { "sender_sig", "Patrick & Éadaoin" },
                { "reset_link", resetLink },
                { "display_name", user.DisplayName }
            });

        await SendMailjetEmailAsync(request);
    }

    public async Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
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
                    { "Name", user.DisplayName }
                }
            })
            .Property("Mj-TemplateID", 8136101)
            .Property("Mj-TemplateLanguage", true)
            .Property("Variables", new JObject
            {
                { "sender_sig", "Patrick & Éadaoin" },
                { "reset_code", resetCode },
                { "display_name", user.DisplayName }
            });

        await SendMailjetEmailAsync(request);
    }
}
