using CF.Events.API;
using CF.Events.API.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var startup = new Startup(builder.Configuration, builder.Environment);

    builder.ConfigureLogging();

    startup.ConfigureServices(builder.Services);

    var app = builder.Build();

    await startup.EnsureDatabaseSeeded(app.Services);

    startup.Configure(app);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during api bootstrapping");
}
finally
{
    Log.CloseAndFlush();
}
