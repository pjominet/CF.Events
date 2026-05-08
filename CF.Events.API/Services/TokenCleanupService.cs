using CF.Events.API.Data;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.API.Services;

public class TokenCleanupService(IServiceProvider services, ILogger<TokenCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

                var now = DateTime.UtcNow;
                var expiredTokens = await db.RevokedTokens
                    .Where(t => t.ExpiryDate < now)
                    .ToListAsync(stoppingToken);

                if (expiredTokens.Count > 0)
                {
                    db.RevokedTokens.RemoveRange(expiredTokens);
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Cleaned up {Count} expired revoked tokens", expiredTokens.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during token cleanup");
            }

            // Run once an hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
