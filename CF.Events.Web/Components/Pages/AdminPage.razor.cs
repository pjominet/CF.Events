using CF.Events.Web.Services;
using CF.Events.Web.Components.Layout;
using CF.Events.Shared;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace CF.Events.Web.Components.Pages;

public partial class AdminPage : ComponentBase
{
    private bool isLoading = true;
    private bool isRefreshing;
    private readonly Dictionary<int, ElementReference> copyBadgeRefs = [];
    private List<Rsvp> rsvps = [];
    private int totalAttendance;
    private int totalDinner;
    private bool isApiOffline;
    private string apiError = string.Empty;
    private ConfirmationDialog deleteConfirmation = null!;
    private RsvpDetailModal detailModal = null!;
    private int? rsvpIdToDelete;
    private Rsvp? selectedRsvp;

    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await LoadRsvps();
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

    private async Task LoadRsvps()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated is not true)
        {
            NavigationManager.NavigateTo("account/login");
            return;
        }

        var tokenValueResult = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
        if (string.IsNullOrEmpty(tokenValueResult))
        {
            NavigationManager.NavigateTo("account/login");
            return;
        }

        isRefreshing = true;
        var startTime = DateTime.UtcNow;

        var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenValueResult);

        try
        {
            var result = await client.GetFromJsonAsync<List<Rsvp>>("api/events/engagement/rsvp");
            if (result is not null)
            {
                rsvps = result.OrderByDescending(r => r.SubmittedAt).ToList();
                totalAttendance = rsvps.Count(r => r.Attending) + rsvps.Count(r => r is { Attending: true, BringsPlusOne: true });
                totalDinner = rsvps.Count(r => r is { Attending: true, JoinForDinner: true }) + rsvps.Count(r => r is { Attending: true, JoinForDinner: true, BringsPlusOne: true });
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
        finally
        {
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds < 500)
            {
                await Task.Delay(500 - (int)elapsed.TotalMilliseconds);
            }
            isRefreshing = false;
        }
    }

    private async Task Logout()
    {
        var tokenValueResult = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
        if (!string.IsNullOrEmpty(tokenValueResult))
        {
            try
            {
                var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenValueResult);
                await client.PostAsync("api/auth/logout", null);
            }
            catch
            {
                // Ignore logout errors
            }
        }

        await ((ApiAuthenticationStateProvider)AuthStateProvider).MarkUserAsLoggedOut();
        NavigationManager.NavigateTo("account/login");
    }

    private async Task CopyCode(string code, int rsvpId)
    {
        if (copyBadgeRefs.TryGetValue(rsvpId, out var element))
        {
            await JsRuntime.InvokeVoidAsync("copyToClipboard", code, element);
            ToastService.Show("Code copied to clipboard!", ToastType.Success);
        }
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
        var tokenValueResult = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
        if (string.IsNullOrEmpty(tokenValueResult)) return;

        var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenValueResult);

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
}
