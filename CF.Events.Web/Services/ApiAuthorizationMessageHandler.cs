using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

namespace CF.Events.Web.Services;

public class ApiAuthorizationMessageHandler(AuthenticationStateProvider authStateProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await ((ApiAuthenticationStateProvider)authStateProvider).GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
