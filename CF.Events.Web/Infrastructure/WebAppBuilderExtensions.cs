using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace CF.Events.Web.Infrastructure;

public static class WebAppBuilderExtensions
{
    public static void ConfigureLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", builder.Environment.IsDevelopment() ? LogEventLevel.Information : LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .WriteTo.File(
                "logs/cf-events-.log",
                restrictedToMinimumLevel: LogEventLevel.Information,
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Host.UseSerilog();
    }
}
