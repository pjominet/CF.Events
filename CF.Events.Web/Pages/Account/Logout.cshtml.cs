using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CF.Events.Web.Pages.Account;

[Authorize]
public class LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger) : PageModel
{
    public IActionResult OnGet() => RedirectToPage("./Login");

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var username = HttpContext.User.Identity?.Name;
        await signInManager.SignOutAsync();
        logger.LogInformation("User {Username} logged out", username);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("./Login");
    }
}
