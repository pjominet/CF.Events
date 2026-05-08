using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace CF.Events.Web.Components.Pages;

public partial class AdminPage : ComponentBase
{
    private bool isLoading = true;
    private bool showSetup = false;
    private bool showLogin = false;
    private string setupPassword = "";
    private string confirmPassword = "";
    private string setupError = "";
    private string loginPassword = "";
    private string loginError = "";
    private List<Rsvp> rsvps = new();
    private int totalAttendance = 0;
    private int totalDinner = 0;
    private bool isApiOffline = false;
    private string apiError = "";
    private string? token;

    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ToastService ToastService { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                token = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", "adminToken");
                if (string.IsNullOrEmpty(token))
                {
                    await CheckSetupStatus();
                }
                else
                {
                    await LoadRsvps();
                }
            }
            catch (Exception ex)
            {
                isApiOffline = true;
                apiError = "Unable to connect to the API. Please ensure the server is running.";
                Console.WriteLine($"Initialization error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }
    }

    private async Task CheckSetupStatus()
    {
        var client = HttpClientFactory.CreateClient("EventsAPI");
        try
        {
            var status = await client.GetFromJsonAsync<SetupStatus>("api/events/engagement/setup-status");
            if (status?.NeedsSetup == true)
            {
                showSetup = true;
                showLogin = false;
            }
            else
            {
                showSetup = false;
                showLogin = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking setup status: {ex.Message}");
            throw; // Re-throw to be caught in OnAfterRenderAsync
        }
    }

    private async Task HandleSetup()
    {
        if (setupPassword != confirmPassword)
        {
            setupError = "Passwords do not match.";
            return;
        }

        var client = HttpClientFactory.CreateClient("EventsAPI");
        try
        {
            var response = await client.PostAsync($"api/events/engagement/setup?password={Uri.EscapeDataString(setupPassword)}", null);
            if (response.IsSuccessStatusCode)
            {
                ToastService.Show("Admin setup successful! Please login.", ToastType.Success);
                showSetup = false;
                showLogin = true;
                setupPassword = "";
                confirmPassword = "";
            }
            else
            {
                setupError = await response.Content.ReadAsStringAsync();
            }
        }
        catch (Exception ex)
        {
            setupError = "Could not connect to server.";
        }
    }

    private async Task HandleLogin()
    {
        var client = HttpClientFactory.CreateClient("EventsAPI");
        try
        {
            var response = await client.PostAsync($"api/events/engagement/login?password={Uri.EscapeDataString(loginPassword)}", null);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                token = result?.Token;
                if (!string.IsNullOrEmpty(token))
                {
                    await JSRuntime.InvokeVoidAsync("sessionStorage.setItem", "adminToken", token);
                    showLogin = false;
                    await LoadRsvps();
                }
            }
            else
            {
                loginError = "Invalid password.";
            }
        }
        catch (Exception ex)
        {
            loginError = "Login failed.";
            ToastService.Show("Login failed. Connection error.", ToastType.Error);
        }
    }

    private async Task LoadRsvps()
    {
        if (string.IsNullOrEmpty(token)) return;

        var client = HttpClientFactory.CreateClient("EventsAPI");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var result = await client.GetFromJsonAsync<List<Rsvp>>("api/events/engagement/rsvp");
            if (result != null)
            {
                rsvps = result.OrderByDescending(r => r.SubmittedAt).ToList();
                totalAttendance = rsvps.Count(r => r.Attending) + rsvps.Where(r => r.Attending && r.BringsPlusOne).Count();
                totalDinner = rsvps.Count(r => r.Attending && r.JoinForDinner);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading RSVPs: {ex.Message}");
            if (ex is HttpRequestException { StatusCode: System.Net.HttpStatusCode.Unauthorized })
            {
                await Logout();
            }
            else
            {
                isApiOffline = true;
                apiError = "Lost connection to the API.";
                ToastService.Show("Error loading data. API might be offline.", ToastType.Error);
            }
        }
    }

    private async Task Logout()
    {
        await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "adminToken");
        token = null;
        showLogin = true;
        rsvps.Clear();
        StateHasChanged();
    }

    private class SetupStatus { public bool NeedsSetup { get; set; } }
    private class LoginResult { public string? Token { get; set; } }
}
