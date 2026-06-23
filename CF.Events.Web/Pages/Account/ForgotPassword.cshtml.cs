using System.ComponentModel.DataAnnotations;
using System.Text;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace CF.Events.Web.Pages.Account;

[AllowAnonymous]
public class ForgotPasswordModel(UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            // Don't reveal that the user does not exist.
            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        // Self-contained app: no email delivery, so redirect directly to the reset page with the token.
        var code = await userManager.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        return RedirectToPage("./ResetPassword", new { code, email = Input.Email });
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;
    }
}
