using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CF.Events.Web.Providers;

public class ApiAuthorizationMessageHandler(AuthenticationStateProvider authStateProvider, NavigationManager navigationManager) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var provider = (ApiAuthenticationStateProvider)authStateProvider;
        var token = await provider.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is not HttpStatusCode.Unauthorized) return response;

        await provider.MarkUserAsLoggedOut();
        navigationManager.NavigateTo("account/login");

        return response;
    }
}
