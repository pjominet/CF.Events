namespace CF.Events.Web.Infrastructure.Middlewares;

public class SecurityHeadersMiddleware(RequestDelegate next, string? contentSecurityPolicy = null)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

        if (!string.IsNullOrEmpty(contentSecurityPolicy))
            context.Response.Headers.Append("Content-Security-Policy", contentSecurityPolicy);

        await next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, string contentSecurityPolicy)
        => app.UseMiddleware<SecurityHeadersMiddleware>(contentSecurityPolicy);
}
