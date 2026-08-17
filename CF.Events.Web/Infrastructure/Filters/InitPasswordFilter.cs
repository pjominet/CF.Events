using CF.Events.Web.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CF.Events.Web.Infrastructure.Filters;

public class InitPasswordFilter(LinkGenerator linkGenerator) : IAsyncPageFilter, IAsyncActionFilter
{
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            await next();
            return;
        }

        if (ShouldRedirect(context.HttpContext))
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToPageResult("/Account/Manage/FirstLogin", new { returnUrl });
            return;
        }

        await next();
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            await next();
            return;
        }

        if (ShouldRedirect(context.HttpContext))
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToPageResult("/Account/Manage/FirstLogin", new { returnUrl });
            return;
        }

        await next();
    }

    private bool ShouldRedirect(HttpContext httpContext)
    {
        var user = httpContext.User;

        // 1. Check if the user is authenticated
        if (user.Identity?.IsAuthenticated != true) return false;

        // 2. Check if the MustChangePassword flag is set
        if (!user.InitPassword()) return false;

        // 3. Avoid infinite loops: check if we are already on the FirstLogin page or logging out
        var path = httpContext.Request.Path.Value ?? string.Empty;

        var excludedPages = new[]
        {
            "/Account/Manage/FirstLogin",
            "/Account/Logout"
        };

        return excludedPages
            .Select(page => linkGenerator.GetPathByPage(page))
            .All(excludedPath => string.IsNullOrEmpty(excludedPath) || !path.Equals(excludedPath, StringComparison.OrdinalIgnoreCase));
    }
}
