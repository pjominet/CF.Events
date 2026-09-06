using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CF.Events.Web.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/account/email-login");

        if (User.InitPassword())
            return Redirect("/account/manage/first-login");

        return User.IsAdmin()
            ? Redirect("/admin")
            : Redirect("/invites");
    }
}
