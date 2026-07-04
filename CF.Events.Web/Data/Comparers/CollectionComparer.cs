using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CF.Events.Web.Data.Comparers;

public class CollectionComparer<T>() : ValueComparer<T[]>(
    (a, b) => Compare(a, b),
    v => ComputeHashCode(v),
    v => CreateSnapshot(v) ?? Array.Empty<T>())
    where T : struct, Enum
{
    private static bool Compare(T[]? a, T[]? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a.Length != b.Length) return false;

        return !a.Where((t, i) => !EqualityComparer<T>.Default.Equals(t, b[i])).Any();
    }

    private static int ComputeHashCode(T[]? array)
    {
        if (array is null) return 0;

        var hash = new HashCode();
        foreach (var item in array)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }

    private static T[]? CreateSnapshot(T[]? array)
    {
        if (array is null) return null;
        var copy = new T[array.Length];
        Array.Copy(array, copy, array.Length);
        return copy;
    }
}
