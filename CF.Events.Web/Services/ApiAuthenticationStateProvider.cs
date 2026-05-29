using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace CF.Events.Web.Services;

public class ApiAuthenticationStateProvider(ProtectedLocalStorage localStorage) : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal anonymous = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var userSessionResult = await localStorage.GetAsync<string>("authToken");
            var token = userSessionResult.Success ? userSessionResult.Value : null;

            if (string.IsNullOrWhiteSpace(token))
                return new AuthenticationState(anonymous);

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "Bearer");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch (InvalidOperationException)
        {
            // JavaScript interop is not available during prerendering.
            return new AuthenticationState(anonymous);
        }
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        try
        {
            await localStorage.SetAsync("authToken", token);
        }
        catch (InvalidOperationException)
        {
            // JavaScript interop is not available during prerendering.
        }

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "Bearer");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        try
        {
            await localStorage.DeleteAsync("authToken");
        }
        catch (InvalidOperationException)
        {
            // JavaScript interop is not available during prerendering.
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
    }

    internal async Task<string?> GetTokenAsync()
    {
        try
        {
            var result = await localStorage.GetAsync<string>("authToken");
            return result.Success ? result.Value : null;
        }
        catch (InvalidOperationException)
        {
            // JavaScript interop is not available during prerendering.
            return null;
        }
    }

    private static List<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs is null) return claims;

        keyValuePairs.TryGetValue(ClaimTypes.Role, out var roles);

        if (roles is not null)
        {
            var rolesString = roles.ToString()!.Trim();
            if (rolesString.StartsWith('['))
            {
                var parsedRoles = JsonSerializer.Deserialize<string[]>(rolesString);
                claims.AddRange(parsedRoles?.Select(parsedRole => new Claim(ClaimTypes.Role, parsedRole)) ?? []);
            }
            else claims.Add(new Claim(ClaimTypes.Role, rolesString));

            keyValuePairs.Remove(ClaimTypes.Role);
        }

        claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!)));

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}
