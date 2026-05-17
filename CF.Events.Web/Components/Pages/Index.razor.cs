using CF.Events.Web.Services;
using CF.Events.Shared;
using static CF.Events.Shared.Constants;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CF.Events.Web.Components.Pages;

public partial class Index : ComponentBase
{
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
}
