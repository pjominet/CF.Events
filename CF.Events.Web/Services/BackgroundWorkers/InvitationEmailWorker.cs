using CF.Events.Web.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Services.BackgroundWorkers;

public class InvitationEmailWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AppSettings> appSettings,
    ILogger<InvitationEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Invitation Email Worker is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var emailInvitationService = scope.ServiceProvider.GetRequiredService<IInvitationService>();
                await emailInvitationService.ProcessPendingEmails(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while processing pending invitation emails");
            }

            var intervalHours = appSettings.Value.EmailBatchIntervalHours ?? 24;
            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }

        logger.LogInformation("Invitation Email Worker is stopping");
    }
}
