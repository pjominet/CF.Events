namespace CF.Events.Web.Infrastructure;

public static class Constants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Guest = "Guest";
    }

    public static class ProviderNames
    {
        public const string EmailConfirmation = "EmailConfirmation";
    }

    public static class ViewDataKeys
    {
        public const string ShowAddModal = "ShowAddModal";
        public const string ShowEventModal = "ShowEventModal";
        public const string ImportErrors = "ImportErrors";
    }
}
