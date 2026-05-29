using CF.Events.Shared;
using CF.Events.Shared.DTOs;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CF.Events.Web.Components.Pages.Account;

public partial class Register
{
    [Inject] private AuthService AuthService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private readonly RegisterRequest registerRequest = new();
    private string? errorMessage;
    private bool isSubmitting;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated is true)
        {
            NavigationManager.NavigateTo(user.IsInRole(Constants.Roles.Admin) ? "admin/events" : "invites");
        }
    }

    private async Task HandleRegister()
    {
        errorMessage = null;
        isSubmitting = true;

        var result = await AuthService.RegisterAsync(registerRequest);
        isSubmitting = false;

        if (result.Success)
        {
            ToastService.Show("Registration successful. Please login.", ToastType.Success);
            NavigationManager.NavigateTo("account/login");
        }
        else errorMessage = result.Error;
    }
}
