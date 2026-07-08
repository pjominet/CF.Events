using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace CF.Events.Web.Services;

public class IdentityEmailSender(IEmailProvider emailProvider) : IEmailSender<AppUser>
{
    public async Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "sender_sig", "Patrick & Éadaoin" },
            { "confirmation_link", confirmationLink },
            { "display_name", user.DisplayName! }
        };

        await emailProvider.SendTemplatedEmailAsync("8135949", email, variables);
    }

    public async Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "sender_sig", "Patrick & Éadaoin" },
            { "reset_link", resetLink },
            { "display_name", user.DisplayName! }
        };

        await emailProvider.SendTemplatedEmailAsync("8136026", email, variables);
    }

    public async Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
    {
        var variables = new Dictionary<string, string>
        {
            { "sender_sig", "Patrick & Éadaoin" },
            { "reset_code", resetCode },
            { "display_name", user.DisplayName! }
        };

        await emailProvider.SendTemplatedEmailAsync("8136101", email, variables);
    }
}
