using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace CF.Events.Web.Services;

public class IdentityEmailSender(IEmailProvider emailProvider) : IIdentityEmailSender
{
    public async Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "app_name", "P&E Wedding" },
            { "confirm_url", confirmationLink },
            { "user_name", user.DisplayName! }
        };

        await emailProvider.SendTemplatedEmailAsync("0838936", email, variables);
    }

    public async Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "app_name", "P&E Wedding" },
            { "reset_url", resetLink }
        };

        await emailProvider.SendTemplatedEmailAsync("0670355", email, variables);
    }

    public async Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode) => throw new NotImplementedException();

    public async Task SendLoginLinkAsync(AppUser user, string email, string loginLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "sender_sig", "Patrick & Éadaoin" },
            { "app_name", "P&E Wedding" },
            { "user_name", user.DisplayName ?? user.UserName ?? string.Empty },
            { "login_url", loginLink }
        };

        await emailProvider.SendTemplatedEmailAsync("0670355", email, variables);
    }
}
