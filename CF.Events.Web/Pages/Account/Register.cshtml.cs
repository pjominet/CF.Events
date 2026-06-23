using System.ComponentModel.DataAnnotations;
using System.Text;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Attributes;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace CF.Events.Web.Pages.Account;

[AllowAnonymous]
public class RegisterModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailSender<ApplicationUser> emailSender,
    ILogger<RegisterModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
            return Page();

        var isFirstUser = !userManager.Users.Any();

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            DisplayName = Input.DisplayName
        };

        var result = await userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        logger.LogInformation("User created a new account with password");

        await userManager.AddToRoleAsync(user, isFirstUser ? Constants.Roles.Admin : Constants.Roles.User);

        // First user: auto-confirm and sign in
        if (isFirstUser)
        {
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
            await signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl ?? "/");
        }

        // Non-first users: send confirmation email, do NOT sign in
        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var callbackUrl = Url.Page(
            "./EmailConfirmation",
            pageHandler: null,
            values: new { userId = user.Id, code },
            protocol: Request.Scheme)!;

        await emailSender.SendConfirmationLinkAsync(user, Input.Email, callbackUrl);

        return RedirectToPage("./RegisterConfirmation");
    }

    public sealed class InputModel
    {
        [Required]
        [Display(Name = "Display Name")]
        public string DisplayName { get; init; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; init; } = string.Empty;

        [Required]
        [PasswordValidation]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; init; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}
