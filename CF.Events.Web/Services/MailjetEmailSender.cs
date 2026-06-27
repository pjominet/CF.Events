using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace CF.Events.Web.Services;

public class MailjetEmailSender(MailjetService mailjetService) : IEmailSender<ApplicationUser>, IEmailSender
{
    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        => await mailjetService.SendConfirmationLinkAsync(user.DisplayName ?? user.UserName ?? "undefined", email, confirmationLink);

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        => await mailjetService.SendPasswordResetLinkAsync(user.DisplayName ?? user.UserName ?? "undefined", email, resetLink);

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        => await mailjetService.SendPasswordResetCodeAsync(user.DisplayName ?? user.UserName ?? "undefined", email, resetCode);

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        => await mailjetService.SendEmailAsync(email, subject, htmlMessage);
}
