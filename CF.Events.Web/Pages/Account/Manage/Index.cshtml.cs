using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CF.Events.Web.Pages.Account.Manage;

[Authorize]
public class IndexModel(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager) : PageModel
{
    public string? Username { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return NotFound("Unable to load user.");

        Username = await userManager.GetUserNameAsync(user);
        Input.DisplayName = user.DisplayName;
        Input.PhoneNumber = await userManager.GetPhoneNumberAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return NotFound("Unable to load user.");

        if (!ModelState.IsValid)
        {
            Username = await userManager.GetUserNameAsync(user);
            return Page();
        }

        var phoneNumber = await userManager.GetPhoneNumberAsync(user);
        if (Input.PhoneNumber != phoneNumber)
            await userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);

        if (Input.DisplayName != user.DisplayName)
        {
            user.DisplayName = Input.DisplayName;
            await userManager.UpdateAsync(user);
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["Toast"] = "Your profile has been updated";
        TempData["ToastType"] = "success";
        return RedirectToPage();
    }

    public sealed class InputModel
    {
        [Display(Name = "Display name")]
        public string? DisplayName { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }
}
