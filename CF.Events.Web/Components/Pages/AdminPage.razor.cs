using CF.Events.Web.Services;
using CF.Events.Web.Components.Layout;
using CF.Events.Shared;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace CF.Events.Web.Components.Pages;

public partial class AdminPage : ComponentBase
{
    private bool isLoading = true;
    private bool showSetup;
    private bool showLogin;
    private string setupPassword = string.Empty;
    private string confirmPassword = string.Empty;
    private string setupError = string.Empty;
    private string loginPassword = string.Empty;
    private string loginError = string.Empty;
    private readonly Dictionary<int, ElementReference> copyBadgeRefs = [];
    private List<Rsvp> rsvps = [];
    private int totalAttendance;
    private int totalDinner;
    private bool isApiOffline;
    private string apiError = string.Empty;
    private string? token;
    private ConfirmationDialog deleteConfirmation = null!;
    private RsvpDetailModal detailModal = null!;
    private int? rsvpIdToDelete;
    private Rsvp? selectedRsvp;

    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                token = await JsRuntime.InvokeAsync<string>("sessionStorage.getItem", "adminToken");
                if (string.IsNullOrEmpty(token))
                    await CheckSetupStatus();
                else await LoadRsvps();
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
        var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
        try
        {
            var status = await client.GetFromJsonAsync<SetupStatus>("api/events/engagement/setup-status");
            if (status?.NeedsSetup is true)
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

        var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
        try
        {
            var response = await client.PostAsJsonAsync("api/events/engagement/setup", new LoginRequest { Password = setupPassword });
            if (response.IsSuccessStatusCode)
            {
                ToastService.Show("Admin setup successful! Please login.", ToastType.Success);
                showSetup = false;
                showLogin = true;
                setupPassword = string.Empty;
                confirmPassword = string.Empty;
            }
            else setupError = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            setupError = "Could not connect to server.";
        }
    }

    private async Task HandleLogin()
    {
        var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
        try
        {
            var response = await client.PostAsJsonAsync("api/events/engagement/login", new LoginRequest { Password = loginPassword });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                token = result?.Token;
                if (!string.IsNullOrEmpty(token))
                {
                    await JsRuntime.InvokeVoidAsync("sessionStorage.setItem", "adminToken", token);
                    showLogin = false;
                    await LoadRsvps();
                }
            }
            else loginError = "Invalid password.";
        }
        catch
        {
            loginError = "Login failed.";
            ToastService.Show("Login failed. Connection error.", ToastType.Error);
        }
    }

    private async Task LoadRsvps()
    {
        if (string.IsNullOrEmpty(token)) return;

        var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var result = await client.GetFromJsonAsync<List<Rsvp>>("api/events/engagement/rsvp");
            if (result is not null)
            {
                copyBadgeRefs.Clear();
                rsvps = result.OrderByDescending(r => r.SubmittedAt).ToList();
                totalAttendance = rsvps.Count(r => r.Attending) + rsvps.Count(r => r is { Attending: true, BringsPlusOne: true });
                totalDinner = rsvps.Count(r => r is { Attending: true, JoinForDinner: true });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading RSVPs: {ex.Message}");
            if (ex is HttpRequestException { StatusCode: System.Net.HttpStatusCode.Unauthorized })
                await Logout();
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
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await client.PostAsync("api/events/engagement/logout", null);
            }
            catch
            {
                // Ignore logout errors
            }
        }

        await JsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "adminToken");
        token = null;
        showLogin = true;
        rsvps.Clear();
        StateHasChanged();
    }

    private async Task CopyCode(string code, ElementReference element)
    {
        await JsRuntime.InvokeVoidAsync("copyToClipboard", code, element);
        ToastService.Show("Code copied to clipboard!", ToastType.Success);
    }

    private void ShowDetails(Rsvp rsvp)
    {
        selectedRsvp = rsvp;
        detailModal.Show();
    }

    private void RequestDelete(int id)
    {
        rsvpIdToDelete = id;
        deleteConfirmation.Show();
    }

    private async Task HandleDeleteConfirmation(bool confirmed)
    {
        if (confirmed && rsvpIdToDelete.HasValue)
            await DeleteRsvp(rsvpIdToDelete.Value);
        rsvpIdToDelete = null;
    }

    private async Task DeleteRsvp(int id)
    {
        if (string.IsNullOrEmpty(token)) return;

        var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.DeleteAsync($"api/events/engagement/rsvp/admin/{id}");
            if (response.IsSuccessStatusCode)
            {
                ToastService.Show("RSVP deleted successfully.", ToastType.Success);
                await LoadRsvps();
            }
            else ToastService.Show("Failed to delete RSVP.", ToastType.Error);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting RSVP: {ex.Message}");
            ToastService.Show("Error deleting RSVP. Connection error.", ToastType.Error);
        }
    }

    private class SetupStatus { public bool NeedsSetup { get; init; } }
    private class LoginResult { public string? Token { get; init; } }
}
