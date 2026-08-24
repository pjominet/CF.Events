using System.Security.Claims;

namespace CF.Events.Web.Infrastructure.Extensions;

public static class ClaimsIdentityExtensions
{
    /// <param name="identity">The <see cref="ClaimsIdentity"/> to try to add the claims to</param>
    extension(ClaimsIdentity identity)
    {
        /// <summary>
        /// Adds a claim only if the claim type does not yet exist
        /// </summary>
        /// <param name="type">Type (key) of the claim</param>
        /// <param name="value">Claim value</param>
        /// <returns>True if the claim has been added, false if a claim of the same type existed already</returns>
        public bool TryAddClaim(string type, string value)
        {
            if (identity.HasClaim(type, value))
                return false;

            identity.AddClaim(new Claim(type, value));
            return true;
        }

        /// <summary>
        /// Returns collection of claims by the type
        /// </summary>
        /// <param name="type">Type (key) of the claim</param>
        /// <returns>Collection of claims</returns>
        public IEnumerable<Claim> TryGetClaims(string type)
        {
            return [.. identity.Claims.Where(c => c.Type == type)];
        }

        /// <summary>
        /// Remove claims from identity
        /// </summary>
        /// <param name="claims">Claims to remove</param>
        public void TryRemoveClaims(IEnumerable<Claim> claims)
        {
            claims.ToList().ForEach(identity.RemoveClaim);
        }

        /// <summary>
        /// Remove claims from identity by claims type
        /// </summary>
        /// <param name="type">Type of claims</param>
        public void TryRemoveClaimsByType(string type)
        {
            var claims = identity.TryGetClaims(type);
            identity.TryRemoveClaims(claims);
        }
    }
}
