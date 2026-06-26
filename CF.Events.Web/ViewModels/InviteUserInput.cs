using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.ViewModels;

public sealed class InviteUserInput
{
    [Required]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = "";

    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = "";

    [Required]
    [Display(Name = "Temporary Password")]
    public string Password { get; set; } = "";
}
