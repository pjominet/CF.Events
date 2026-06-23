using CF.Events.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Constants.Roles.Admin)]
public class DashboardModel : PageModel
{
    public void OnGet() { }
}
