using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
namespace CF.Events.Web.Components.Pages;

public partial class RsvpPage : ComponentBase
{
    private Rsvp rsvpModel = new();
    private Rsvp? currentRsvp;
    private bool showResult;
    private bool isLoading = true;
    private string? fingerprint;

    private bool isApiOffline = false;
    private string apiError = "";

    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            fingerprint = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "rsvp_fingerprint");
            if (string.IsNullOrEmpty(fingerprint))
            {
                fingerprint = $"fp_{Guid.NewGuid().ToString("N").Substring(0, 9)}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                await JSRuntime.InvokeVoidAsync("localStorage.setItem", "rsvp_fingerprint", fingerprint);
            }
            rsvpModel.Fingerprint = fingerprint;
            try
            {
                await CheckStatus();
            }
            catch (Exception ex)
            {
                isApiOffline = true;
                apiError = "Unable to connect to the RSVP service. Please try again later.";
                Console.WriteLine($"RSVP init error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }
    }

    private async Task CheckStatus()
    {
        if (string.IsNullOrEmpty(fingerprint)) return;

        var client = HttpClientFactory.CreateClient("EventsAPI");
        try
        {
            var response = await client.GetAsync($"api/events/engagement/rsvp/check/{fingerprint}");
            if (response.IsSuccessStatusCode)
            {
                currentRsvp = await response.Content.ReadFromJsonAsync<Rsvp>();
                showResult = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Status check failed: {ex.Message}");
            throw; // Re-throw to be caught in OnAfterRenderAsync
        }
    }

    private async Task HandleSubmit()
    {
        var client = HttpClientFactory.CreateClient("EventsAPI");
        try
        {
            HttpResponseMessage response;
            if (rsvpModel.Id > 0)
                response = await client.PutAsJsonAsync($"api/events/engagement/rsvp/{rsvpModel.Id}", rsvpModel);
            else response = await client.PostAsJsonAsync("api/events/engagement/rsvp", rsvpModel);

            if (response.IsSuccessStatusCode)
            {
                currentRsvp = await response.Content.ReadFromJsonAsync<Rsvp>();
                showResult = true;
                ToastService.Show(rsvpModel.Id > 0 ? "Your RSVP has been updated!" : "Thank you for your response!", ToastType.Success);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                ToastService.Show("You have already RSVP'd.", ToastType.Info);
                await CheckStatus();
            }
            else ToastService.Show("Something went wrong. Please try again.", ToastType.Error);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error submitting RSVP: {ex.Message}");
            ToastService.Show("Could not connect to the server. Please try again later.", ToastType.Error);
        }
    }

    private void ModifyRsvp()
    {
        if (currentRsvp is null) return;

        rsvpModel = new Rsvp
        {
            Id = currentRsvp.Id,
            Name = currentRsvp.Name,
            Attending = currentRsvp.Attending,
            BringsPlusOne = currentRsvp.BringsPlusOne,
            JoinForDinner = currentRsvp.JoinForDinner,
            Comments = currentRsvp.Comments,
            Fingerprint = currentRsvp.Fingerprint
        };
        showResult = false;
    }

    private async Task DeleteRsvp()
    {
        if (currentRsvp is null) return;

        var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete your RSVP?");
        if (!confirmed) return;

        var client = HttpClientFactory.CreateClient("EventsAPI");
        try
        {
            var response = await client.DeleteAsync($"api/events/engagement/rsvp/{currentRsvp.Id}?fingerprint={fingerprint}");
            if (response.IsSuccessStatusCode)
            {
                ToastService.Show("Your RSVP has been deleted.", ToastType.Success);
                rsvpModel = new Rsvp { Fingerprint = fingerprint! };
                showResult = false;
            }
            else ToastService.Show("Delete failed.", ToastType.Error);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete failed: {ex.Message}");
            ToastService.Show("Delete failed. Connection error.", ToastType.Error);
        }
    }
}
