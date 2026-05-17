using CF.Events.Web.Services;
using CF.Events.Shared;
using static CF.Events.Shared.Constants;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
builder.Services.AddAuthorization();

builder.Services.AddSingleton<ToastService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddHttpClient(HttpClients.EventsApi, client =>
{
    var apiBaseUrl = builder.Configuration["EventsApi:BaseUrl"];
    if (string.IsNullOrEmpty(apiBaseUrl))
        apiBaseUrl = "http://localhost:5041";
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (!ctx.Context.Request.Path.StartsWithSegments("/invitations") || ctx.Context.User.Identity is { IsAuthenticated: true })
            return;

        ctx.Context.Response.StatusCode = 403;
        ctx.Context.Response.Body = Stream.Null;
    }
});

app.UseSecurityHeaders(
    "default-src 'self'; " +
    "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
    "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
    "img-src 'self' data:; " +
    "connect-src 'self' ws: wss: http://localhost:5041;"); // Allow WebSocket for Blazor Server and API connection

app.UseAntiforgery();
app.MapRazorComponents<CF.Events.Web.App>()
    .AddInteractiveServerRenderMode();

app.Run();
