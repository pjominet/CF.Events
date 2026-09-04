namespace CF.Events.Web.Infrastructure.Extensions;

public static class ICollectionExtensions
{
    extension<T>(ICollection<T> enumerable) where T : struct, Enum
    {
        public bool IsIn(params T[] items) => items.Any(enumerable.Contains);

        public bool IsNotIn(params T[] items) => !items.Any(enumerable.Contains);
    }
}
