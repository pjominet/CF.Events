using System.Security.Claims;
using System.Security.Principal;
using CF.Events.Web.Infrastructure.Factories;

namespace CF.Events.Web.Infrastructure.Exceptions;

public static class PrincipalExtensions
{
    public static string GetDisplayName(this IPrincipal currentPrincipal)
    {
        return !string.IsNullOrWhiteSpace(currentPrincipal.GetClaimValue(EventClaims.DisplayName))
            ? currentPrincipal.GetClaimValue(EventClaims.DisplayName)
            : currentPrincipal.Identity?.Name ?? "Unkown Account";
    }

    private static string GetClaimValue(this IPrincipal currentPrincipal, string key)
    {
        return currentPrincipal.Identity is ClaimsIdentity identity
            ? identity.Claims.Where(c => c.Type == key).Select(c => c.Value).FirstOrDefault() ?? string.Empty
            : string.Empty;
    }
}
