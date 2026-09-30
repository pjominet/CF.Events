namespace CF.Events.Web.Infrastructure;

public static class Constants
{
    public enum RoleOrder
    {
        Sudo = 0,
        Admin = 1,
        User = 2,
        Guest = 3
    }

    public static class Roles
    {
        public const string Sudo = "Sudo";
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
        public const string ImportErrors = "ImportErrors";
    }

    public static class Email
    {
        public const string NonSendableEmail = "no-send.tech";

        public static class IdentityEmailTemplates
        {
            public const string EmailConfirmationLink = "0838936";
            public const string PasswordRestLink = "0670355";
            public const string EmailLoginLink = "0214257";
        }
    }

    public static class RateLimitingPolicy
    {
        public const string EmailLogin = "EmailLogin";
    }

    public static class CacheKeys
    {
        public const string EmailTemplates = "Smtp2Go_EmailTemplates";
    }
}
