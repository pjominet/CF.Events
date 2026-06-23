using System.Text;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace CF.Events.Web.Pages.Account;

[AllowAnonymous]
public class EmailConfirmation(UserManager<ApplicationUser> userManager) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string userId, string code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            return BadRequest();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await userManager.ConfirmEmailAsync(user, code);

        return result.Succeeded
            ? RedirectToPage("./EmailConfirmation")
            : RedirectToPage("./Error");
    }
}
