using System.Security.Claims;
using System.Security.Principal;
using CF.Events.Web.Infrastructure.Factories;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Infrastructure.Extensions;

public static class PrincipalExtensions
{
    extension(IPrincipal currentPrincipal)
    {
        public bool IsAuthenticated() => currentPrincipal.Identity?.IsAuthenticated == true;
        public bool IsAdmin() => currentPrincipal.IsInRole(Roles.Admin);
        public bool IsUser() => currentPrincipal.IsInRole(Roles.User);
        public string GetId() => currentPrincipal.GetClaimValue(ClaimTypes.NameIdentifier);
        public string GetEmail() => currentPrincipal.GetClaimValue(ClaimTypes.Name);
        public string GetDisplayName() => currentPrincipal.GetClaimValue(EventClaims.DisplayName);

        private string GetClaimValue(string key)
        {
            return currentPrincipal.Identity is ClaimsIdentity identity
                ? identity.Claims.Where(c => c.Type == key).Select(c => c.Value).FirstOrDefault() ?? string.Empty
                : string.Empty;
        }
    }
}
