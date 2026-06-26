using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace CF.Events.Web.Services;

// No-op email sender. The application is self-contained and does not send emails;
// confirmation/reset links are surfaced in the UI instead.
internal sealed class NoOpEmailSender : IEmailSender<ApplicationUser>, IEmailSender
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        => Task.CompletedTask;

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        => Task.CompletedTask;

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        => Task.CompletedTask;

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
        => Task.CompletedTask;
}
