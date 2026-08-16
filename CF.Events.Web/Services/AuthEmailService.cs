using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Services;

public interface IAuthEmailService
{
    Task SendLoginEmailAsync(AppUser user, CancellationToken ctx = default);
    Task<string> CreateAuthCodeAsync(string userId, int? eventId, int validityDays, CancellationToken ctx = default);
    string BuildAuthCallbackUrl(string code, int? eventId);
}

public class AuthEmailService(
    EventsDbContext db,
    IIdentityEmailSender emailSender,
    IOptions<AppSettings> appOptions) : IAuthEmailService
{
    private readonly AppSettings _appSettings = appOptions.Value;

    public async Task SendLoginEmailAsync(AppUser user, CancellationToken ctx = default)
    {
        var code = await CreateAuthCodeAsync(user.Id, null, 1, ctx);
        var callbackUrl = BuildAuthCallbackUrl(code, null);

        await emailSender.SendLoginLinkAsync(user, user.Email!, callbackUrl);
    }

    public async Task<string> CreateAuthCodeAsync(string userId, int? eventId, int validityDays, CancellationToken ctx = default)
    {
        var code = CodeGenerator.Generate(64);
        await db.AuthCodes.AddAsync(new AuthCode
        {
            UserId = userId,
            EventId = eventId,
            Value = code,
            ValidUntil = DateTime.UtcNow.AddDays(validityDays)
        }, ctx);

        await db.SaveChangesAsync(ctx);
        return code;
    }

    public string BuildAuthCallbackUrl(string code, int? eventId)
    {
        var baseUrl = _appSettings.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/account/auth-callback?code={code}";
        if (eventId.HasValue)
            url += $"&eventId={eventId.Value}";
        return url;
    }
}
