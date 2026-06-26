using System.Security.Claims;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Infrastructure.Factories;

public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        _ = identity.TryAddClaim(EventClaims.DisplayName, user.DisplayName ?? string.Empty);
        _ = identity.TryAddClaim(EventClaims.InitPassword, user.MustChangePassword.ToString());

        return identity;
    }
}

internal static class EventClaims
{
    public const string DisplayName = "displayName";
    public const string InitPassword = "init_pw";
}
