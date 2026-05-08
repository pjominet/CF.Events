using CF.Events.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ToastService>();

builder.Services.AddHttpClient("EventsAPI", client =>
{
    var isHttps = builder.Environment.IsDevelopment(); // Simplified for now
    client.BaseAddress = new Uri(isHttps ? "https://localhost:7041" : "http://localhost:5041");
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
