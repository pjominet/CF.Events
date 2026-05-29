using CF.Events.Web.Services;
using CF.Events.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CF.Events.Web.Components.Pages.Admin;

public partial class Users : ComponentBase
{
    private bool isLoading = true;
    private bool isInviting;
    private bool showInviteModal;
    private RegisterRequest registerRequest = new();
    private List<UserDto> allUsers = [];

    [Inject] private AuthService AuthService { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        GeneratePassword();
        await LoadUsers();
    }

    private async Task LoadUsers()
    {
        isLoading = true;
        allUsers = await AuthService.GetUsersAsync();
        isLoading = false;
    }

    private void ShowInviteModal()
    {
        registerRequest = new RegisterRequest();
        GeneratePassword();
        showInviteModal = true;
    }

    private void CloseInviteModal() => showInviteModal = false;

    private void EditUser(UserDto user)
    {
        // Placeholder for edit functionality
        ToastService.Show($"Edit user {user.Email} - Not implemented yet.", ToastType.Info);
    }

    private void GeneratePassword()
    {
        registerRequest.Password = Guid.NewGuid().ToString("N")[..10];
    }

    private async Task HandleInviteUser()
    {
        isInviting = true;
        var email = registerRequest.Email;
        var result = await AuthService.RegisterAsync(registerRequest);
        isInviting = false;

        if (result.Success)
        {
            ToastService.Show($"Invitation sent to {email}", ToastType.Success);
            showInviteModal = false;
            await LoadUsers();
        }
        else
        {
            ToastService.Show(result.Error ?? "Failed to invite user", ToastType.Error);
        }
    }
}
