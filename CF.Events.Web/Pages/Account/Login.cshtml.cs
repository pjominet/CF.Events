using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Data;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CF.Events.Web.Infrastructure.Extensions;

namespace CF.Events.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    EventsDbContext db,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public bool ShowPasswordStep { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(string? email = null, string? returnUrl = null)
    {
        // Clear any existing external cookie to ensure a clean login process.
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        if (!userManager.Users.Any())
            return RedirectToPage("./Register");

        if (email.HasValue(false))
            Input.Email = email;

        ReturnUrl = returnUrl ?? "/";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? ReturnUrl;

        if (!ShowPasswordStep)
        {
            ModelState.Remove("Input.Password");
            if (!ModelState.IsValid)
                return Page();

            var user = await userManager.FindByEmailAsync(Input.Email);
            if (user is { MustChangePassword: true })
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                user.LastLogin = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
                logger.LogInformation("User {UserName} logged in for first login", user.UserName);

                db.LoginAudits.Add(new LoginAudit
                {
                    UserId = user.Id,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    AuthMethod = "First Login"
                });
                await db.SaveChangesAsync();

                return RedirectToPage("./Manage/FirstLogin", new { returnUrl = ReturnUrl });
            }

            ShowPasswordStep = true;
            ModelState.Clear();
            return Page();
        }

        if (!Input.Password.HasValue())
            ModelState.AddModelError("Input.Password", "The Password field is required.");

        if (!ModelState.IsValid)
            return Page();

        var result = await signInManager.PasswordSignInAsync(Input.Email, Input.Password!, Input.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await userManager.FindByEmailAsync(Input.Email);
            if (user is null)
                return NotFound();

            user.LastLogin = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
            logger.LogInformation("User {UserName} logged in", user.UserName);

            // Log the login audit
            db.LoginAudits.Add(new LoginAudit
            {
                UserId = user.Id,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                AuthMethod = "Password"
            });
            await db.SaveChangesAsync();

            if (user.MustChangePassword)
                return RedirectToPage("./Manage/FirstLogin", new { returnUrl = ReturnUrl });

            return LocalRedirect(ReturnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User account locked out");
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return Page();
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; init; }
    }
}
