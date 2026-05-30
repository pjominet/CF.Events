using System.Net;
using CF.Events.Shared.DTOs;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
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
            var client = HttpClientFactory.CreateClient(HttpClients.EventsApi);

            var response = await client.GetAsync($"events/{EventId}");
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
            var client = HttpClientFactory.CreateClient(HttpClients.EventsApi);

            var response = await client.GetAsync($"events/{EventId}/invitation-content");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<InvitationContentDto>();
                processedHtml = content?.HtmlContent ?? "<div class='alert alert-warning'>Invitation content is empty.</div>";
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                processedHtml = "<div class='alert alert-danger'>You do not have permission to view this invitation.</div>";
            }
            else processedHtml = "<div class='alert alert-warning'>Invitation content not found or access denied.</div>";
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
            try
            {
                // Hook the RSVP button via JS
                await JS.InvokeVoidAsync("hookRsvpButton", "rsvp-button", $"events/{eventData.Id}/rsvp");
            }
            catch (InvalidOperationException)
            {
                // JavaScript interop calls cannot be issued at this time.
            }
        }
    }

    public class EventDetailDto
    {
        public Event Event { get; init; } = null!;
        public Rsvp? Rsvp { get; init; }
    }
}
