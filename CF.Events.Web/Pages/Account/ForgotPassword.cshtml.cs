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
public class ForgotPasswordModel(
    UserManager<AppUser> userManager,
    IEmailSender<AppUser> emailSender,
    IWebHostEnvironment environment) : PageModel
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

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        // Generate the reset link
        var callbackUrl = Url.Page(
            "/account/reset-password",
            pageHandler: null,
            values: new { token, email = Input.Email },
            protocol: Request.Scheme)!;

        if (environment.IsDevelopment())
            TempData["ResetPasswordLink"] = callbackUrl;
        else await emailSender.SendPasswordResetLinkAsync(user, Input.Email, callbackUrl);

        return RedirectToPage("./ForgotPasswordConfirmation");
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;
    }
}
