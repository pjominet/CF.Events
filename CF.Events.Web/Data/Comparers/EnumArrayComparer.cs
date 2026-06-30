using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CF.Events.Web.Data.Comparers;

public class EnumArrayComparer<TEnum>() : ValueComparer<TEnum[]>(
    (a, b) => Compare(a, b),
    v => ComputeHashCode(v),
    v => CreateSnapshot(v) ?? Array.Empty<TEnum>())
    where TEnum : struct, Enum
{
    private static bool Compare(TEnum[]? a, TEnum[]? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a.Length != b.Length) return false;

        return !a.Where((t, i) => !EqualityComparer<TEnum>.Default.Equals(t, b[i])).Any();
    }

    private static int ComputeHashCode(TEnum[]? array)
    {
        if (array is null) return 0;

        var hash = new HashCode();
        foreach (var item in array)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }

    private static TEnum[]? CreateSnapshot(TEnum[]? array)
    {
        if (array is null) return null;
        var copy = new TEnum[array.Length];
        Array.Copy(array, copy, array.Length);
        return copy;
    }
}
