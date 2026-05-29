using Microsoft.AspNetCore.Mvc;

namespace CF.Events.API.Infrastructure.Attributes;

public class ApiRouteAttribute : RouteAttribute
{
    private const string BaseRoute = "api";

    public ApiRouteAttribute(string template) : base($"{BaseRoute}/{template}") { }
    public ApiRouteAttribute() : base($"{BaseRoute}") { }
}
