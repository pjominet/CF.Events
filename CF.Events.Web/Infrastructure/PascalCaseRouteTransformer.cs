using System.Text.RegularExpressions;

namespace CF.Events.Web.Infrastructure;

public partial class PascalCaseRouteTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
        => value is not null ? OutboundRouteRegex().Replace(value.ToString()!, "$1-$2").ToLower() : null;

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex OutboundRouteRegex();
}
