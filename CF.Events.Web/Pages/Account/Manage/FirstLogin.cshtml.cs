using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CF.Events.Web.Pages.Account.Manage;

[Authorize]
public class FirstLoginModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return NotFound("Unable to load user.");

        ReturnUrl = returnUrl;

        if (!user.MustChangePassword && !string.IsNullOrEmpty(user.DisplayName))
            return LocalRedirect(returnUrl ?? "/");

        Input.DisplayName = user.DisplayName ?? "";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return NotFound("Unable to load user.");

        if (!ModelState.IsValid)
            return Page();

        user.DisplayName = Input.DisplayName;
        user.MustChangePassword = false;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            foreach (var error in removeResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        var addResult = await userManager.AddPasswordAsync(user, Input.NewPassword);
        if (!addResult.Succeeded)
        {
            foreach (var error in addResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        await signInManager.RefreshSignInAsync(user);
        return LocalRedirect(ReturnUrl ?? "/");
    }

    public sealed class InputModel
    {
        [Required]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
