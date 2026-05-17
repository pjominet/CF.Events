using CF.Events.Web.Services;
using CF.Events.Shared.Models;
using CF.Events.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using static CF.Events.Shared.Constants;

namespace CF.Events.Web.Components.Pages.Admin;

public partial class Users : ComponentBase
{
    private bool isInviting;
    private RegisterRequest registerRequest = new();

    [Inject] private AuthService AuthService { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;

    protected override void OnInitialized()
    {
        GeneratePassword();
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
            registerRequest = new RegisterRequest();
            GeneratePassword();
        }
        else
        {
            ToastService.Show(result.Error ?? "Failed to invite user", ToastType.Error);
        }
    }
}
