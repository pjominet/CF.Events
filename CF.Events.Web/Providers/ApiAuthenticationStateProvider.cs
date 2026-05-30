using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace CF.Events.Web.Providers;

public class ApiAuthenticationStateProvider(
    ProtectedLocalStorage localStorage,
    PersistentComponentState state,
    IHttpContextAccessor httpContextAccessor) : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal anonymous = new(new ClaimsIdentity());
    private string? cachedToken;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // 1. Try to get from PersistentComponentState (available during first interactive render after SSR)
        if (state.TryTakeFromJson<string>("authToken", out var persistedToken))
        {
            cachedToken = persistedToken;
            return CreateAuthenticationState(persistedToken!);
        }

        // 2. Try to get from Cookie (available during SSR if we set it up)
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var token = httpContext.Request.Cookies["authToken"];
            if (!string.IsNullOrWhiteSpace(token))
            {
                cachedToken = token;
                return CreateAuthenticationState(token);
            }
        }

        // 3. Try to get from LocalStorage (available during interactive mode)
        try
        {
            var userSessionResult = await localStorage.GetAsync<string>("authToken");
            var token = userSessionResult.Success ? userSessionResult.Value : null;

            if (!string.IsNullOrWhiteSpace(token))
            {
                cachedToken = token;
                return CreateAuthenticationState(token);
            }
        }
        catch (InvalidOperationException)
        {
            // JavaScript interop is not available during prerendering.
        }

        return new AuthenticationState(anonymous);
    }

    private AuthenticationState CreateAuthenticationState(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return new AuthenticationState(anonymous);
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "Bearer");
        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        cachedToken = token;
        try
        {
            await localStorage.SetAsync("authToken", token);
        }
        catch (InvalidOperationException)
        {
            // JavaScript interop is not available during prerendering.
        }

        NotifyAuthenticationStateChanged(Task.FromResult(CreateAuthenticationState(token)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        cachedToken = null;
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

    public async Task<string?> GetTokenAsync()
    {
        if (cachedToken is not null) return cachedToken;

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var token = httpContext.Request.Cookies["authToken"];
            if (!string.IsNullOrWhiteSpace(token))
            {
                cachedToken = token;
                return cachedToken;
            }
        }

        try
        {
            var result = await localStorage.GetAsync<string>("authToken");
            cachedToken = result.Success ? result.Value : null;
            return cachedToken;
        }
        catch (InvalidOperationException)
        {
            // JavaScript interop is not available during prerendering.
            return null;
        }
    }

    private static List<Claim> ParseClaimsFromJwt(string jwt)
    {
        try
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            var claims = new List<Claim>();
            if (keyValuePairs is null) return claims;

            ExtractClaim(keyValuePairs, claims, ClaimTypes.Role, ClaimTypes.Role, "role", "roles");
            ExtractClaim(keyValuePairs, claims, ClaimTypes.Name, ClaimTypes.Name, "unique_name");
            ExtractClaim(keyValuePairs, claims, ClaimTypes.NameIdentifier, ClaimTypes.NameIdentifier, "sub");

            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!)));

            return claims;
        }
        catch
        {
            return [];
        }
    }

    private static void ExtractClaim(Dictionary<string, object> keyValuePairs, List<Claim> claims, string targetClaimType, params string[] sourceKeys)
    {
        foreach (var key in sourceKeys)
        {
            if (!keyValuePairs.TryGetValue(key, out var value)) continue;

            var valueString = value.ToString()!.Trim();
            if (valueString.StartsWith('['))
            {
                try
                {
                    var parsedValues = JsonSerializer.Deserialize<string[]>(valueString);
                    claims.AddRange(parsedValues?.Select(v => new Claim(targetClaimType, v)) ?? []);
                }
                catch
                {
                    claims.Add(new Claim(targetClaimType, valueString));
                }
            }
            else claims.Add(new Claim(targetClaimType, valueString));

            foreach (var k in sourceKeys) keyValuePairs.Remove(k);
            return;
        }
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
