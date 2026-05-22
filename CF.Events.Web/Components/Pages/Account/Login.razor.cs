using CF.Events.Shared;
using CF.Events.Shared.DTOs;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CF.Events.Web.Components.Pages.Account;

public partial class Login
{
    [Inject] private AuthService AuthService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private readonly LoginRequest loginRequest = new();
    private string? errorMessage;
    private bool isSubmitting;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated is true)
        {
            if (user.IsInRole(Constants.Roles.Admin))
            {
                NavigationManager.NavigateTo("admin/events");
            }
            else
            {
                NavigationManager.NavigateTo("invites");
            }
        }
    }

    private async Task HandleLogin()
    {
        errorMessage = null;
        isSubmitting = true;

        var result = await AuthService.LoginAsync(loginRequest);
        isSubmitting = false;

        if (result is { Success: true, Token: not null })
        {
            await ((ApiAuthenticationStateProvider)AuthStateProvider).MarkUserAsAuthenticated(result.Token);

            if (result.MustChangePassword)
            {
                NavigationManager.NavigateTo("account/setup");
            }
            else
            {
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                if (authState.User.IsInRole(Constants.Roles.Admin))
                {
                    NavigationManager.NavigateTo("admin/events");
                }
                else
                {
                    NavigationManager.NavigateTo("invites");
                }
            }
        }
        else errorMessage = result.Error ?? "Invalid login attempt.";
    }
}
