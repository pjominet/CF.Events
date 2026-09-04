using Microsoft.Extensions.Options;
using System.Net;
using CF.Events.Web.Infrastructure.Extensions;

namespace CF.Events.Web.Infrastructure.Settings;

public class AppSettingsValidator : IValidateOptions<AppSettings>
{
    public ValidateOptionsResult Validate(string? name, AppSettings options)
    {
        if (!options.BaseUrl.HasValue())
            return ValidateOptionsResult.Fail("AppSettings:BaseUrl is required.");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uriResult)
            || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            return ValidateOptionsResult.Fail($"AppSettings:BaseUrl '{options.BaseUrl}' is not a valid HTTP/HTTPS URL.");

        return ValidateOptionsResult.Success;
    }
}
