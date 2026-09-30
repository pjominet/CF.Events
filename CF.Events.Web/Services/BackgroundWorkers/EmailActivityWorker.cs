namespace CF.Events.Web.Services.BackgroundWorkers;

public class EmailActivityWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailActivityWorker> logger) : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email Activity Worker is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Email Activity Worker is fetching email activity for the last 24 hours");
                using var scope = scopeFactory.CreateScope();
                var emailActivityService = scope.ServiceProvider.GetRequiredService<IEmailActivityService>();
                var result = await emailActivityService.FetchAndSaveActivityAsync(24, stoppingToken);
                logger.LogInformation(
                    "Email Activity Worker sync completed. Events fetched: {TotalFetched}, Emails processed: {Processed}, New events added: {NewEvents}, Updated: {Updated}",
                    result.TotalEventsFetched,
                    result.EmailsProcessed,
                    result.NewEventsAdded,
                    result.UpdatedEmailsCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // Graceful shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching and saving email activity in background worker");
            }

            try
            {
                await Task.Delay(SyncInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // Graceful shutdown
            }
        }

        logger.LogInformation("Email Activity Worker is stopping");
    }
}
