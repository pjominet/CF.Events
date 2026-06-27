using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CF.Events.Web.ViewModels;

public class InviteUserViewModel
{
    public List<SelectListItem> AvailableUsers { get; init; } = [];

    [Required]
    public int EventId { get; set; }

    [Required]
    public List<string> SelectedUserIds { get; set; } = [];
}
