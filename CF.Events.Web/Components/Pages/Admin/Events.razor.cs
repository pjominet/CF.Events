using CF.Events.Web.Services;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static CF.Events.Shared.Constants;

namespace CF.Events.Web.Components.Pages.Admin;

public partial class Events : ComponentBase
{
    private bool isLoading = true;
    private bool isCreatingEvent;
    private bool showCreateModal;
    private Event newEvent = new() { Date = DateTime.Today.AddMonths(1) };
    private List<string> invitationFiles = [];
    private List<Event> allEvents = [];
    private string inviteEmail = string.Empty;
    private int selectedEventId;

    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        isLoading = true;
        try
        {
            var token = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
            var client = HttpClientFactory.CreateClient(HttpClients.EventsApi);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var eventsResponse = await client.GetAsync("events/all");
            if (eventsResponse.IsSuccessStatusCode)
            {
                allEvents = await eventsResponse.Content.ReadFromJsonAsync<List<Event>>() ?? [];
            }
        }
        catch (Exception ex)
        {
            ToastService.Show($"Error loading data: {ex.Message}", ToastType.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task ShowCreateModal()
    {
        showCreateModal = true;
        if (invitationFiles.Count == 0)
        {
            var token = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
            var client = HttpClientFactory.CreateClient(HttpClients.EventsApi);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("events/invitation-files");
            if (response.IsSuccessStatusCode)
            {
                invitationFiles = await response.Content.ReadFromJsonAsync<List<string>>() ?? [];
            }
        }
    }
    private void CloseCreateModal() => showCreateModal = false;

    private async Task HandleCreateEvent()
    {
        isCreatingEvent = true;
        try
        {
            var token = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
            var client = HttpClientFactory.CreateClient(HttpClients.EventsApi);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("events", newEvent);
            if (response.IsSuccessStatusCode)
            {
                ToastService.Show("Event created successfully!", ToastType.Success);
                newEvent = new Event
                {
                    Date = DateTime.Today.AddMonths(1)
                };
                showCreateModal = false;
                await LoadData();
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

    private async Task ToggleEventStatus(Event ev)
    {
        ev.IsActive = !ev.IsActive;
        try
        {
            var token = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
            var client = HttpClientFactory.CreateClient(HttpClients.EventsApi);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsJsonAsync($"events/{ev.Id}", ev);
            if (!response.IsSuccessStatusCode)
            {
                ev.IsActive = !ev.IsActive;
                ToastService.Show("Failed to update event status", ToastType.Error);
            }
            else
            {
                ToastService.Show($"Event {(ev.IsActive ? "activated" : "deactivated")} successfully", ToastType.Success);
            }
        }
        catch (Exception ex)
        {
            ev.IsActive = !ev.IsActive;
            ToastService.Show(ex.Message, ToastType.Error);
        }
    }

    private async Task HandleInviteToEvent()
    {
        if (selectedEventId == 0 || string.IsNullOrEmpty(inviteEmail))
        {
            ToastService.Show("Please select an event and provide an email", ToastType.Info);
            return;
        }

        try
        {
            var token = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
            var client = HttpClientFactory.CreateClient(HttpClients.EventsApi);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync($"events/{selectedEventId}/invite", inviteEmail);
            if (response.IsSuccessStatusCode)
            {
                ToastService.Show($"User invited successfully", ToastType.Success);
                inviteEmail = string.Empty;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                ToastService.Show(error, ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show(ex.Message, ToastType.Error);
        }
    }
}
