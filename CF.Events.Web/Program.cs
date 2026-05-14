using CF.Events.Web.Services;
using CF.Events.Shared;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ToastService>();

builder.Services.AddHttpClient(Constants.HttpClients.EventsApi, client =>
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

app.UseStaticFiles();

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
