using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;

namespace CF.Events.Web.Controllers;

[Route("account")]
public class AccountController(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IToastNotification toastNotification,
    ILogger<AccountController> logger) : Controller
{
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout([FromForm] string? returnUrl = null)
    {
        var username = HttpContext.User.Identity?.Name;
        await signInManager.SignOutAsync();
        logger.LogInformation("User {Username} logged out", username);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("/account/login");
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return BadRequest();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        var result = await userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
            return RedirectToPage("/error");

        await signInManager.SignInAsync(user, isPersistent: true);

        toastNotification.AddSuccessToastMessage("Email successfully confirmed");
        return LocalRedirect("/");
    }
}
