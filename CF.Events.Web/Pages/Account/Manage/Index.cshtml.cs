using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Account.Manage;

[Authorize]
public class IndexModel(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ILogger<IndexModel> logger) : PageModel
{
    public string? Username { get; set; }

    [BindProperty]
    public ProfileInputModel Profile { get; set; } = new();

    [BindProperty]
    public ChangePasswordInputModel Password { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ActiveTab { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return NotFound("Unable to load user.");

        Username = await userManager.GetUserNameAsync(user);
        Profile.DisplayName = user.DisplayName;
        Profile.PhoneNumber = await userManager.GetPhoneNumberAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        ActiveTab = "profile";
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return NotFound("Unable to load user.");

        if (!ModelState.IsValid)
        {
            Username = await userManager.GetUserNameAsync(user);
            return Page();
        }

        var phoneNumber = await userManager.GetPhoneNumberAsync(user);
        if (Profile.PhoneNumber != phoneNumber)
            await userManager.SetPhoneNumberAsync(user, Profile.PhoneNumber);

        if (Profile.DisplayName != user.DisplayName)
        {
            user.DisplayName = Profile.DisplayName;
            await userManager.UpdateAsync(user);
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["Toast"] = "Your profile has been updated";
        TempData["ToastType"] = "success";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        ActiveTab = "password";
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return NotFound("Unable to load user.");

        if (User.IsGuest())
            return BadRequest("Guest users cannot change password.");

        if (!ModelState.IsValid)
        {
            Username = await userManager.GetUserNameAsync(user);
            Profile.DisplayName = user.DisplayName;
            Profile.PhoneNumber = await userManager.GetPhoneNumberAsync(user);
            return Page();
        }

        var result = await userManager.ChangePasswordAsync(user, Password.OldPassword, Password.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            Username = await userManager.GetUserNameAsync(user);
            Profile.DisplayName = user.DisplayName;
            Profile.PhoneNumber = await userManager.GetPhoneNumberAsync(user);
            return Page();
        }

        await signInManager.RefreshSignInAsync(user);
        logger.LogInformation("User {UserName} changed their password successfully", user.UserName);
        TempData["Toast"] = "Your password has been changed";
        TempData["ToastType"] = "success";
        return RedirectToPage();
    }

    public sealed class ProfileInputModel
    {
        [Display(Name = "Display name")]
        public string? DisplayName { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }

    public sealed class ChangePasswordInputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; } = "";

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
