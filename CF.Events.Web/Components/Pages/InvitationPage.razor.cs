using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CF.Events.Shared.Models;
using CF.Events.Web.Services;
using static CF.Events.Shared.Constants;

namespace CF.Events.Web.Components.Pages;

public partial class InvitationPage
{
    [Parameter] public int EventId { get; set; }
    private bool isLoading = true;
    private Event? eventData;
    private string processedHtml = string.Empty;

    [Inject] private IHttpClientFactory HttpClientFactory { get; init; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; init; } = null!;
    [Inject] private NavigationManager NavigationManager { get; init; } = null!;
    [Inject] private IJSRuntime JS { get; init; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadEvent();
    }

    private async Task LoadEvent()
    {
        isLoading = true;
        try
        {
            var token = await ((ApiAuthenticationStateProvider)AuthStateProvider).GetTokenAsync();
            var client = HttpClientFactory.CreateClient(HttpClients.EventsApi);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"api/events/{EventId}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<EventDetailDto>();
                eventData = result?.Event;

                if (eventData is not null && !string.IsNullOrEmpty(eventData.InvitationFileName))
                {
                    await LoadInvitationHtml();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadInvitationHtml()
    {
        try
        {
            // We fetch the static HTML from wwwroot via HttpClient
            var baseUrl = NavigationManager.BaseUri;
            var htmlUrl = $"{baseUrl}invitations/{eventData!.InvitationFileName}";
            var rawHtml = await new HttpClient().GetStringAsync(htmlUrl);

            // Replace placeholders
            processedHtml = rawHtml
                .Replace("[EventDate]", eventData.Date.ToString("MMMM dd, yyyy"))
                .Replace("[EventLocation]", eventData.Location ?? "To be announced")
                .Replace("[EventName]", eventData.Name);

            // Note: Since we are using Blazor Server, we'll need to hook the button after render
        }
        catch (Exception ex)
        {
            processedHtml = $"<div class='alert alert-danger'>Failed to load invitation: {ex.Message}</div>";
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (eventData is not null && !string.IsNullOrEmpty(processedHtml))
        {
            // Hook the RSVP button via JS
            await JS.InvokeVoidAsync("hookRsvpButton", "rsvp-button", $"events/{eventData.Id}/rsvp");
        }
    }

    public class EventDetailDto
    {
        public Event Event { get; init; } = null!;
        public Rsvp? Rsvp { get; init; }
    }
}
