using System.ComponentModel.DataAnnotations;
using System.Text;
using CF.Events.Web.Infrastructure.Attributes;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace CF.Events.Web.Pages.Account;

[AllowAnonymous]
public class ResetPasswordModel(UserManager<AppUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet(string? token = null, string? email = null)
    {
        if (token is null)
            return BadRequest("A code must be supplied for password reset.");

        Input.Token = token;
        Input.Email = email ?? string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            // Don't reveal that the user does not exist.
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Token));
        var result = await userManager.ResetPasswordAsync(user, token, Input.Password);
        if (result.Succeeded)
            return RedirectToPage("./ResetPasswordConfirmation");

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        return Page();
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [PasswordValidation]
        [DataType(DataType.Password)]
        public string Password { get; init; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; init; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
