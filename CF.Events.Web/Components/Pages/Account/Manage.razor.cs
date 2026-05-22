using System.Security.Claims;
using CF.Events.Shared.DTOs;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CF.Events.Web.Components.Pages.Account;

public partial class Manage
{
    [Inject] private AuthService AuthService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;

    private UpdatePasswordRequest passwordRequest = new();
    private string? errorMessage;
    private string? successMessage;
    private bool isSubmitting;
    private string userEmail = string.Empty;
    private List<string> userRoles = [];

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated is true)
        {
            userEmail = user.Identity.Name ?? "Unknown";
            userRoles = user.Claims.Where(c => c.Type is ClaimTypes.Role).Select(c => c.Value).ToList();
        }
        else
        {
            NavigationManager.NavigateTo("account/login");
        }
    }

    private async Task HandleChangePassword()
    {
        errorMessage = null;
        successMessage = null;
        isSubmitting = true;

        var token = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            errorMessage = "Session expired. Please login again.";
            isSubmitting = false;
            return;
        }

        var result = await AuthService.ChangePasswordAsync(passwordRequest, token);
        isSubmitting = false;

        if (result.Success)
        {
            successMessage = "Password changed successfully.";
            passwordRequest = new UpdatePasswordRequest();
            ToastService.Show("Password changed successfully.", ToastType.Success);
        }
        else
            errorMessage = result.Error ?? "Failed to change password.";
    }

    private async Task HandleLogout()
    {
        var provider = (ApiAuthenticationStateProvider)AuthStateProvider;
        var token = await provider.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            await AuthService.LogoutAsync(token);
        }
        await provider.MarkUserAsLoggedOut();
        NavigationManager.NavigateTo("/account/login");
    }
}
