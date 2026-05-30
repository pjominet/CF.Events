using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CF.Events.Web.Providers;

public class PersistingServerAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly PersistentComponentState state;
    private readonly AuthenticationStateProvider authenticationStateProvider;
    private readonly PersistingComponentStateSubscription subscription;

    private Task<AuthenticationState>? authenticationStateTask;

    public PersistingServerAuthenticationStateProvider(
        PersistentComponentState state,
        ApiAuthenticationStateProvider apiAuthenticationStateProvider)
    {
        this.state = state;
        this.authenticationStateProvider = apiAuthenticationStateProvider;

        authenticationStateTask = this.authenticationStateProvider.GetAuthenticationStateAsync();
        this.authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        subscription = this.state.RegisterOnPersisting(OnPersistingAsync);
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        authenticationStateTask = task;
    }

    private async Task OnPersistingAsync()
    {
        if (authenticationStateTask is null)
        {
            return;
        }

        var authenticationState = await authenticationStateTask;
        var principal = authenticationState.User;

        if (principal.Identity?.IsAuthenticated is true)
        {
            var token = await ((ApiAuthenticationStateProvider)authenticationStateProvider).GetTokenAsync();
            if (token is not null)
            {
                state.PersistAsJson("authToken", token);
            }
            else
            {
                Console.WriteLine("[DEBUG_LOG] OnPersistingAsync: Token is null, cannot persist");
            }
        }
        else
        {
            Console.WriteLine("[DEBUG_LOG] OnPersistingAsync: User is NOT authenticated");
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => authenticationStateProvider.GetAuthenticationStateAsync();

    public void Dispose()
    {
        subscription.Dispose();
        ((ApiAuthenticationStateProvider)authenticationStateProvider).AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}
