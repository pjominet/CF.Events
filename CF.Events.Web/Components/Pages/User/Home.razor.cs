using CF.Events.Web.Services;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using static CF.Events.Shared.Constants;

namespace CF.Events.Web.Components.Pages.User;

public partial class Home : ComponentBase
{
    private bool isLoading = true;
    private List<UserInviteDto> myInvites = [];

    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadInvites();
        if (myInvites.Count == 1)
        {
            NavigationManager.NavigateTo($"events/{myInvites[0].Event.Id}/invitation");
        }
    }

    private async Task LoadInvites()
    {
        isLoading = true;
        try
        {
            var client = HttpClientFactory.CreateClient(HttpClients.EventsApi);

            var response = await client.GetAsync("events");
            if (response.IsSuccessStatusCode)
            {
                myInvites = await response.Content.ReadFromJsonAsync<List<UserInviteDto>>() ?? [];
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                NavigationManager.NavigateTo("account/login");
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

    public class UserInviteDto
    {
        public Event Event { get; set; } = null!;
        public Rsvp Rsvp { get; set; } = null!;
    }
}
