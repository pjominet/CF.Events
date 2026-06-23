namespace CF.Events.Web.Infrastructure.Middlewares;

public class UrlTransformerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        if (path.HasValue && !path.Value.Equals(path.Value.ToLowerInvariant()))
        {
            context.Response.Redirect(path.Value.ToLowerInvariant());
            return;
        }

        await next(context);
    }
}

public static class UrlTransformerExtensions
{
    public static IApplicationBuilder UseUrlTransformer(this IApplicationBuilder app)
        => app.UseMiddleware<UrlTransformerMiddleware>();
}
