using CF.Events.Web.Services;
using CF.Events.Web.Components.Layout;
using CF.Events.Shared;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
namespace CF.Events.Web.Components.Pages;

public partial class RsvpPage : ComponentBase
{
    private Rsvp rsvpModel = new();
    private Rsvp? currentRsvp;
    private bool showResult;
    private bool isLoading = true;
    private string? savedAccessCode;
    private string accessCodeInput = string.Empty;
    private bool isRetrieving;
    private ElementReference copyBadgeElement;
    private ConfirmationDialog deleteConfirmation = null!;

    private bool isApiOffline;
    private string apiError = string.Empty;

    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private ToastService ToastService { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            savedAccessCode = await JsRuntime.InvokeAsync<string>("localStorage.getItem", "rsvp_access_code");
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
        if (string.IsNullOrEmpty(savedAccessCode)) return;

        var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
        try
        {
            var response = await client.GetAsync($"api/events/engagement/rsvp/code/{savedAccessCode}");
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

    private async Task RetrieveByCode()
    {
        if (string.IsNullOrEmpty(accessCodeInput)) return;

        isRetrieving = true;
        var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
        try
        {
            var response = await client.GetAsync($"api/events/engagement/rsvp/code/{accessCodeInput.Trim().ToUpper()}");
            if (response.IsSuccessStatusCode)
            {
                currentRsvp = await response.Content.ReadFromJsonAsync<Rsvp>();
                if (currentRsvp is not null)
                {
                    // Sync access code to local storage for future seamless access
                    savedAccessCode = currentRsvp.AccessCode;
                    await JsRuntime.InvokeVoidAsync("localStorage.setItem", "rsvp_access_code", savedAccessCode);
                    showResult = true;
                    StateHasChanged();
                    ToastService.Show("RSVP found and synchronized to this device!", ToastType.Success);
                }
            }
            else
            {
                ToastService.Show("RSVP not found. Please check the code.", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Retrieval failed: {ex.Message}");
            ToastService.Show("Connection error. Please try again.", ToastType.Error);
        }
        finally
        {
            isRetrieving = false;
        }
    }

    private async Task HandleSubmit()
    {
        var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
        try
        {
            HttpResponseMessage response;
            if (rsvpModel.Id > 0)
                response = await client.PutAsJsonAsync($"api/events/engagement/rsvp/{rsvpModel.Id}", rsvpModel);
            else response = await client.PostAsJsonAsync("api/events/engagement/rsvp", rsvpModel);

            if (response.IsSuccessStatusCode)
            {
                currentRsvp = await response.Content.ReadFromJsonAsync<Rsvp>();
                if (currentRsvp is not null)
                {
                    savedAccessCode = currentRsvp.AccessCode;
                    await JsRuntime.InvokeVoidAsync("localStorage.setItem", "rsvp_access_code", savedAccessCode);
                }
                showResult = true;
                StateHasChanged(); // Force UI update
                ToastService.Show(rsvpModel.Id > 0 ? "Your RSVP has been updated!" : "Thank you for your response!", ToastType.Success);
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
            AccessCode = currentRsvp.AccessCode
        };
        showResult = false;
        StateHasChanged();
    }

    private async Task CopyCode(string code)
    {
        await JsRuntime.InvokeVoidAsync("copyToClipboard", code, copyBadgeElement);
        ToastService.Show("Code copied to clipboard!", ToastType.Success);
    }

    private async Task HandleDeleteConfirmation(bool confirmed)
    {
        if (confirmed) await DeleteRsvp();
    }

    private async Task DeleteRsvp()
    {
        if (currentRsvp is null) return;

        var client = HttpClientFactory.CreateClient(Constants.HttpClients.EventsApi);
        try
        {
            var response = await client.DeleteAsync($"api/events/engagement/rsvp/{currentRsvp.Id}?accessCode={currentRsvp.AccessCode}");
            if (response.IsSuccessStatusCode)
            {
                ToastService.Show("Your RSVP has been deleted.", ToastType.Success);
                await JsRuntime.InvokeVoidAsync("localStorage.removeItem", "rsvp_access_code");
                savedAccessCode = null;
                rsvpModel = new Rsvp();
                showResult = false;
                StateHasChanged(); // Force UI update
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
