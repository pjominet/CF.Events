using CF.Events.Web.Services;
using CF.Events.Shared;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CF.Events.Web.Components.Pages;

public partial class Index : ComponentBase
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JsRuntime.InvokeVoidAsync("initializeTooltips");
        if (firstRender) await CheckAndRedirect();
    }

    private async Task CheckAndRedirect()
    {
        var fingerprint = await JsRuntime.InvokeAsync<string>("localStorage.getItem", "rsvp_fingerprint");
        var accessCode = await JsRuntime.InvokeAsync<string>("localStorage.getItem", "rsvp_access_code");

        if (string.IsNullOrEmpty(fingerprint) && string.IsNullOrEmpty(accessCode)) return;

        var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);

        try
        {
            HttpResponseMessage? response = null;

            // Try fingerprint first
            if (!string.IsNullOrEmpty(fingerprint))
                response = await client.GetAsync($"api/events/engagement/rsvp/check/{fingerprint}");

            // If not found by fingerprint, but we have an access code, try that
            if (response is not { IsSuccessStatusCode: true } && !string.IsNullOrEmpty(accessCode))
                response = await client.GetAsync($"api/events/engagement/rsvp/code/{accessCode}");

            if (response is { IsSuccessStatusCode: true })
            {
                var rsvp = await response.Content.ReadFromJsonAsync<Rsvp>();
                if (rsvp is not null)
                {
                    // Ensure local storage is in sync
                    await JsRuntime.InvokeVoidAsync("localStorage.setItem", "rsvp_access_code", rsvp.AccessCode);
                    NavigationManager.NavigateTo("engagement/rsvp");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Status check failed: {ex.Message}");
        }
    }
}
