using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CF.Events.Web.Controllers;

[Route("account")]
public class AccountController(SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger) : Controller
{
    [HttpGet("logout")]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        var username = HttpContext.User.Identity?.Name;
        await signInManager.SignOutAsync();
        logger.LogInformation("User {Username} logged out", username);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("/account/login");
    }
}
