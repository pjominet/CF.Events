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
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(anonymous);
        }
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        await localStorage.SetAsync("authToken", token);
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await localStorage.DeleteAsync("authToken");
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
    }

    public async Task<string?> GetTokenAsync()
    {
        var result = await localStorage.GetAsync<string>("authToken");
        return result.Success ? result.Value : null;
    }

    private static List<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        var claims = new List<Claim>();
        if (keyValuePairs is null) return claims;

        if (keyValuePairs.TryGetValue(ClaimTypes.Role, out var roles))
        {
            if (roles.ToString()!.Trim().StartsWith('['))
            {
                var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString()!);
                claims.AddRange(parsedRoles!.Select(role => new Claim(ClaimTypes.Role, role)));
            }
            else claims.Add(new Claim(ClaimTypes.Role, roles.ToString()!));

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
