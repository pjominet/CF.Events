using CF.Events.Web.Services;
using CF.Events.Web.Components.Layout;
using CF.Events.Shared;
using static CF.Events.Shared.Constants;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace CF.Events.Web.Components.Pages;

public partial class RsvpPage : ComponentBase
{
    [Parameter] public int EventId { get; set; }
    private Rsvp rsvpModel = new();
    private Event? eventData;
    private bool isLoading = true;
    private bool isSubmitting;
    private bool isApiOffline;
    private string apiError = string.Empty;

    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadData();
        }
        catch (Exception ex)
        {
            isApiOffline = true;
            apiError = "Unable to connect to the server.";
            Console.WriteLine(ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadData()
    {
        var token = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
        var client = HttpClientFactory.CreateClient(HttpClients.EventsApi);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"api/events/{EventId}");
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<EventRsvpDto>();
            if (data is not null)
            {
                eventData = data.Event;
                rsvpModel = data.Rsvp ?? new Rsvp { EventId = EventId };
            }
        }
        else
        {
            NavigationManager.NavigateTo("/");
        }
    }

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        try
        {
            var token = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
            var client = HttpClientFactory.CreateClient(HttpClients.EventsApi);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync($"api/events/{EventId}/rsvp", rsvpModel);
            if (response.IsSuccessStatusCode)
            {
                ToastService.Show("Thank you for your response!", ToastType.Success);
                NavigationManager.NavigateTo("/");
            }
            else
            {
                ToastService.Show("Failed to submit RSVP.", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            ToastService.Show(ex.Message, ToastType.Error);
        }
        finally
        {
            isSubmitting = false;
        }
    }

    public class EventRsvpDto
    {
        public Event Event { get; set; } = null!;
        public Rsvp? Rsvp { get; set; }
    }
}
