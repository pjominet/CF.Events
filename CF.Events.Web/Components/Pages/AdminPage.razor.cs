using CF.Events.Web.Services;
using CF.Events.Web.Components.Layout;
using CF.Events.Shared;
using static CF.Events.Shared.Constants;
using CF.Events.Shared.Models;
using CF.Events.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CF.Events.Web.Components.Pages;

public partial class AdminPage : ComponentBase
{
    private bool isLoading = true;
    private bool isInviting;
    private bool isCreatingEvent;
    private RegisterRequest registerRequest = new();
    private Event newEvent = new() { Date = DateTime.Today.AddMonths(1) };
    private string apiError = string.Empty;

    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthService AuthService { get; set; } = null!;

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

    private async Task HandleCreateEvent()
    {
        isCreatingEvent = true;
        try
        {
            var token = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
            var client = HttpClientFactory.CreateClient(HttpClients.EventsApi);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("api/events", newEvent);
            if (response.IsSuccessStatusCode)
            {
                ToastService.Show("Event created successfully!", ToastType.Success);
                newEvent = new Event { Date = DateTime.Today.AddMonths(1) };
            }
            else
            {
                ToastService.Show("Failed to create event", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show(ex.Message, ToastType.Error);
        }
        finally
        {
            isCreatingEvent = false;
        }
    }

    private async Task Logout()
    {
        await ((ApiAuthenticationStateProvider)AuthStateProvider).MarkUserAsLoggedOut();
        NavigationManager.NavigateTo("account/login");
    }
}
