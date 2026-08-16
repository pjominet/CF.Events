using System.Security.Claims;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Infrastructure.Factories;

public class ApplicationUserClaimsPrincipalFactory(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<AppUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        _ = identity.TryAddClaim(EventClaims.DisplayName, user.DisplayName ?? string.Empty);

        return identity;
    }
}

internal static class EventClaims
{
    public const string DisplayName = "displayName";
}
