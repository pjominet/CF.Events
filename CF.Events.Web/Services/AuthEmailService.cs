using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Infrastructure.Settings;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Services;

public interface IAuthEmailService
{
    Task SendLoginEmailAsync(AppUser user, CancellationToken ctx = default);
}

public class AuthEmailService(
    EventsDbContext db,
    IIdentityEmailSender emailSender,
    IOptions<AppSettings> appOptions) : IAuthEmailService
{
    private readonly AppSettings _appSettings = appOptions.Value;

    public async Task SendLoginEmailAsync(AppUser user, CancellationToken ctx = default)
    {
        var request = new LoginEmailRequest
        {
            UserId = user.Id,
            UserName = user.DisplayName ?? user.UserName ?? string.Empty,
            UserEmail = user.Email!
        };

        var code = CodeGenerator.Generate(64);
        await db.AuthCodes.AddAsync(new AuthCode
        {
            UserId = request.UserId,
            EventId = request.EventId,
            Value = code,
            ValidUntil = DateTime.UtcNow.AddDays(1)
        }, ctx);

        var baseUrl = _appSettings.BaseUrl.TrimEnd('/');
        request.CallBackUrl = $"{baseUrl}/account/auth-callback?code={code}";
        if (request.EventId.HasValue)
            request.CallBackUrl += $"&eventId={request.EventId.Value}";

        await db.SaveChangesAsync(ctx);

        await emailSender.SendLoginLinkAsync(user, user.Email!, request.CallBackUrl);
    }
}
