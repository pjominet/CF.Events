using CF.Events.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ToastService>();

builder.Services.AddHttpClient("EventsAPI", client =>
{
    var apiBaseUrl = builder.Configuration["EventsApi:BaseUrl"];
    if (string.IsNullOrEmpty(apiBaseUrl))
        apiBaseUrl = "https://localhost:7041";
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<CF.Events.Web.App>()
    .AddInteractiveServerRenderMode();

app.Run();
