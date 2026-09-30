using CF.Events.Web.Infrastructure.Providers.Interfaces;
using CF.Events.Web.Models;
using CF.Events.Web.Services.Interfaces;

namespace CF.Events.Web.Services.EmailSenders;
using static Infrastructure.Constants.Email;

public class IdentityEmailSender(IEmailProvider emailProvider) : IIdentityEmailSender
{
    public async Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "app_name", "E&P Wedding" },
            { "confirm_url", confirmationLink },
            { "user_name", user.DisplayName! }
        };

        await emailProvider.SendTemplatedEmailAsync(new TemplatedEmailEntry(IdentityEmailTemplates.EmailConfirmationLink, email, variables));
    }

    public async Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "app_name", "E&P Wedding" },
            { "reset_url", resetLink }
        };

        await emailProvider.SendTemplatedEmailAsync(new TemplatedEmailEntry(IdentityEmailTemplates.PasswordRestLink, email, variables));
    }

    public async Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode) => throw new NotImplementedException();

    public async Task SendLoginLinkAsync(AppUser user, string email, string loginLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "sender_sig", "Éadaoin & Patrick" },
            { "app_name", "E&P Wedding" },
            { "user_name", user.DisplayName ?? user.UserName ?? string.Empty },
            { "login_url", loginLink }
        };

        await emailProvider.SendTemplatedEmailAsync(new TemplatedEmailEntry(IdentityEmailTemplates.EmailLoginLink, email, variables));
    }
}
