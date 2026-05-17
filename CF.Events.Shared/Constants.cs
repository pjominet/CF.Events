namespace CF.Events.Shared;

public static class Constants
{
    public static class RateLimiting
    {
        public const string Fixed = "fixed";
        public const string Strict = "strict";
    }

    public static class HttpClients
    {
        public const string EventsApi = "EventsAPI";
    }

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }
}
