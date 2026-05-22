using CF.Events.Shared.DTOs;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CF.Events.Web.Components.Pages.Account;

public partial class SetupAccount
{
    [Inject] private AuthService AuthService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;

    private readonly SetupAccountRequest setupRequest = new();
    private string? errorMessage;
    private bool isSubmitting;
    private bool mustChangePassword;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        mustChangePassword = authState.User.FindFirst("must_change_password")?.Value == "true";
    }

    private async Task HandleSetup()
    {
        errorMessage = null;
        isSubmitting = true;

        var token = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            errorMessage = "Session expired. Please login again.";
            isSubmitting = false;
            return;
        }

        var result = await AuthService.SetupAccountAsync(setupRequest, token);
        isSubmitting = false;

        if (result.Success)
        {
            ToastService.Show("Account setup complete!", ToastType.Success);
            NavigationManager.NavigateTo("/");
        }
        else
        {
            errorMessage = result.Error ?? "Failed to complete account setup.";
        }
    }
}
