using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Infrastructure.Attributes;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Account;

[AllowAnonymous]
public class RegisterModel(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IEmailSender<AppUser> emailSender,
    IToastNotification toastNotification,
    IWebHostEnvironment environment,
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

        var user = new AppUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            DisplayName = Input.DisplayName,
            EmailConfirmed = isFirstUser
        };

        var result = await userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        logger.LogInformation("User created a new account with password");

        await userManager.AddToRoleAsync(user, Roles.User);
        if (isFirstUser)
        {
            // First user: auto-add admin role and sign in
            logger.LogInformation("First user created with admin role, no email confirmation required");
            await userManager.AddToRoleAsync(user, Roles.Admin);
            await signInManager.SignInAsync(user, isPersistent: true);
            return LocalRedirect(returnUrl ?? "/");
        }

        // Non-first users: send confirmation email, do NOT sign in
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var callbackUrl = Url.Action(
            "ConfirmEmail", "Account",
            values: new { userId = user.Id, token },
            protocol: Request.Scheme)!;

        if (environment.IsDevelopment())
            TempData["RegistrationLink"] = callbackUrl;
        else await emailSender.SendConfirmationLinkAsync(user, Input.Email, callbackUrl);

        toastNotification.AddSuccessToastMessage("Initial registration successful.");
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
