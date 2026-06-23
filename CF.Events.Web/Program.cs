using CF.Events.Web;
using CF.Events.Web.Infrastructure.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
    });

    var startup = new Startup(builder.Configuration, builder.Environment);

    builder.ConfigureLogging();

    startup.ConfigureServices(builder.Services);

    var app = builder.Build();

    await startup.EnsureDatabase(app.Services);

    startup.Configure(app);

    app.Run();
}
catch (OperationCanceledException)
{
    Log.Information("Application shutdown requested via OperationCanceledException");
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during app bootstrapping");
}
finally
{
    Log.Information("Closing and flushing logger in finally block");
    Log.CloseAndFlush();
}
