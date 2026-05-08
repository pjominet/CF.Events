using CF.Events.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CF.Events.Web.Components.Pages;

public partial class Index : ComponentBase
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ToastService ToastService { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await CheckAndRedirect();
        }
    }

    private async Task CheckAndRedirect()
    {
        var fingerprint = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "rsvp_fingerprint");
        if (string.IsNullOrEmpty(fingerprint)) return;

        var client = HttpClientFactory.CreateClient("EventsAPI");

        try
        {
            var response = await client.GetAsync($"api/events/engagement/rsvp/check/{fingerprint}");
            if (response.IsSuccessStatusCode)
            {
                NavigationManager.NavigateTo("engagement/rsvp");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Status check failed: {ex.Message}");
        }
    }
}
