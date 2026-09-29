using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services;

public interface IEmailActivityService
{
    Task<EmailActivitySyncResult> FetchAndSaveActivityAsync(int hours = 24, CancellationToken ctx = default);
    Task<EmailActivitySyncResult> FetchAndSaveActivityAsync(EmailActivityFetchRequest request, CancellationToken ctx = default);
    Task<List<EmailActivity>> GetRecentActivitiesAsync(int limit = 100, CancellationToken ctx = default);
    Task<EmailActivity?> GetActivityByEmailIdAsync(string emailId, CancellationToken ctx = default);
    Task<List<EmailActivityEvent>> GetTimelineForEmailAsync(string emailId, CancellationToken ctx = default);
}
