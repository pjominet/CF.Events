using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace CF.Events.Web.Services;

public class IdentityEmailSender(IEmailProvider emailProvider) : IEmailSender<AppUser>
{
    public async Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "app_name", "P&E WEDDING" },
            { "confirm_url", confirmationLink },
            { "user_name", user.DisplayName! },
            { "sender_sig", "Patrick & Éadaoin" }
        };

        await emailProvider.SendTemplatedEmailAsync("0838936", email, variables, null);
    }

    public async Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "app_name", "P&E WEDDING" },
            { "reset_link", resetLink },
            { "display_name", user.DisplayName! },
            { "sender_sig", "Patrick & Éadaoin" }
        };

        await emailProvider.SendTemplatedEmailAsync("", email, variables, null);
    }

    public async Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
    {
        var variables = new Dictionary<string, string>
        {
            { "app_name", "P&E WEDDING" },
            { "reset_code", resetCode },
            { "display_name", user.DisplayName! },
            { "sender_sig", "Patrick & Éadaoin" }
        };

        await emailProvider.SendTemplatedEmailAsync("", email, variables, null);
    }
}
