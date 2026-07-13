using CF.Events.Web.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CF.Events.Web.Infrastructure.Filters;

public class InitPasswordFilter : IAsyncPageFilter, IAsyncActionFilter
{
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (ShouldRedirect(context.HttpContext))
        {
            context.Result = new RedirectToPageResult("/Account/Manage/FirstLogin");
            return;
        }

        await next();
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (ShouldRedirect(context.HttpContext))
        {
            context.Result = new RedirectToPageResult("/Account/Manage/FirstLogin");
            return;
        }

        await next();
    }

    private static bool ShouldRedirect(HttpContext httpContext)
    {
        var user = httpContext.User;

        // 1. Check if the user is authenticated
        if (user.Identity?.IsAuthenticated != true) return false;

        // 2. Check if the MustChangePassword flag is set
        if (!user.InitPassword()) return false;

        // 3. Avoid infinite loops: check if we are already on the FirstLogin page or logging out
        var path = httpContext.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Exclude the target page and common exit routes
        return !path.Contains("/account/manage/firstLogin") &&
               !path.Contains("/account/logout");
    }
}
