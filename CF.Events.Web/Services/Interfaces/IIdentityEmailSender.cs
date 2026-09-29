using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace CF.Events.Web.Services.Interfaces;

public interface IIdentityEmailSender : IEmailSender<AppUser>
{
    Task SendLoginLinkAsync(AppUser user, string email, string loginLink);
}
