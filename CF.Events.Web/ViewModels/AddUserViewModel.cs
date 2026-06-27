using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.ViewModels;

public sealed class AddUserViewModel
{
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
