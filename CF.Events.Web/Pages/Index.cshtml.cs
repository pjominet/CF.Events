using CF.Events.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CF.Events.Web.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/account/login");

        return User.IsInRole(Constants.Roles.Admin)
            ? Redirect("/admin/dashbaord")
            : Redirect("/invites");
    }
}
