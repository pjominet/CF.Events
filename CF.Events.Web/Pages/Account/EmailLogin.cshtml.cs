using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting(RateLimitingPolicy.EmailLogin)]
public class EmailLoginModel(
    UserManager<AppUser> userManager,
    IAuthEmailService authEmailService,
    ILogger<EmailLoginModel> logger) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public bool EmailSent { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.FindByEmailAsync(Input.Email);

        if (user is { IsActive: true } && await userManager.IsInRoleAsync(user, Roles.Guest))
        {
            logger.LogInformation("Sending login email to guest user {Email}", Input.Email);
            await authEmailService.SendLoginEmailAsync(user);
        }
        else
        {
            // log failed access attempt
            logger.LogWarning("Invalid user {Email} attempted to use email login flow", Input.Email);
        }

        // don't reveal that the user was not found
        EmailSent = true;
        return Page();
    }

    public class InputModel
    {
        [Required] [EmailAddress] public string Email { get; set; } = string.Empty;
    }
}
