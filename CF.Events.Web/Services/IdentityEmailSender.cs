using CF.Events.Web.Models;
namespace CF.Events.Web.Services;
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

        await emailProvider.SendTemplatedEmailAsync(new EmailEntry(IdentityEmailTemplates.EmailConfirmationLink, email, variables));
    }

    public async Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "app_name", "E&P Wedding" },
            { "reset_url", resetLink }
        };

        await emailProvider.SendTemplatedEmailAsync(new EmailEntry(IdentityEmailTemplates.PasswordRestLink, email, variables));
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

        await emailProvider.SendTemplatedEmailAsync(new EmailEntry(IdentityEmailTemplates.EmailLoginLink, email, variables));
    }
}
