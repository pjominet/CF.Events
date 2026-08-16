using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Account;

[AllowAnonymous]
public class EmailLoginModel(
    UserManager<AppUser> userManager,
    IAuthEmailService authEmailService,
    ILogger<EmailLoginModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool EmailSent { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.FindByEmailAsync(Input.Email);

        // We only allow email auth for Guest users
        if (user is { IsActive: true } && await userManager.IsInRoleAsync(user, Roles.Guest))
        {
            logger.LogInformation("Sending login email to guest user {Email}", Input.Email);
            await authEmailService.SendLoginEmailAsync(user);
        }
        else if (user is not null && await userManager.IsInRoleAsync(user, Roles.Admin))
        {
             logger.LogWarning("Admin user {Email} attempted to use email login flow", Input.Email);
             // We don't send the email for admins, they must use password
             // But we still show the success message to avoid user enumeration
        }

        EmailSent = true;
        return Page();
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
