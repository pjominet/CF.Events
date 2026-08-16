using CF.Events.Web.Models;

namespace CF.Events.Web.Services;

// No-op email sender. The application is self-contained and does not send emails;
// confirmation/reset links are surfaced in the UI instead.
internal sealed class NoOpIdentitySender(ILogger<NoOpIdentitySender> logger) : IIdentityEmailSender
{
    public Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
    {
        logger.LogDebug(
            """
            Fake Confirmation email sent:
                User: {UserName}
                Email: {Email}
                Link: {Link}
            """,
            user.UserName, email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink)
    {
        logger.LogDebug(
            """
            Fake Password Reset link email sent:
                User: {UserName}
                Email: {Email}
                Link: {Link}
            """,
            user.UserName, email, resetLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode) => throw new NotImplementedException();

    public Task SendLoginLinkAsync(AppUser user, string email, string loginLink)
    {
        logger.LogDebug(
            """
            Fake Login link email sent:
                User: {UserName}
                Email: {Email}
                Link: {Link}
            """,
            user.UserName, email, loginLink);
        return Task.CompletedTask;
    }
}
