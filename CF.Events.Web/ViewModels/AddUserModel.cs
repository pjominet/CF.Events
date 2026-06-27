using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.ViewModels;

public sealed class AddUserModel
{
    [Required]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;
}
