using Microsoft.Extensions.Options;
using System.Net;

namespace CF.Events.Web.Infrastructure.Settings;

public class AppSettingsValidator : IValidateOptions<AppSettings>
{
    public ValidateOptionsResult Validate(string? name, AppSettings options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            return ValidateOptionsResult.Fail("AppSettings:BaseUrl is required.");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uriResult)
            || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            return ValidateOptionsResult.Fail($"AppSettings:BaseUrl '{options.BaseUrl}' is not a valid HTTP/HTTPS URL.");

        return ValidateOptionsResult.Success;
    }
}
