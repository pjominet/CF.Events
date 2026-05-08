using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CF.Events.Shared;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _contentSecurityPolicy;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
        _contentSecurityPolicy = null;
    }

    public SecurityHeadersMiddleware(RequestDelegate next, string? contentSecurityPolicy)
    {
        _next = next;
        _contentSecurityPolicy = contentSecurityPolicy;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

        if (!string.IsNullOrEmpty(_contentSecurityPolicy))
            context.Response.Headers.Append("Content-Security-Policy", _contentSecurityPolicy);

        await _next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, string contentSecurityPolicy)
        => app.UseMiddleware<SecurityHeadersMiddleware>(contentSecurityPolicy);
}
