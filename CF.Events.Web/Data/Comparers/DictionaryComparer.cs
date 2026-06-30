using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CF.Events.Web.Data.Comparers;

public class DictionaryComparer<TKey, TValue>() : ValueComparer<Dictionary<TKey, TValue>>(
    (a, b) => Compare(a, b),
    v => ComputeHashCode(v),
    v => CreateSnapshot(v) ?? new Dictionary<TKey, TValue>())
    where TKey : struct, Enum
{
    private static bool Compare(Dictionary<TKey, TValue>? a, Dictionary<TKey, TValue>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        foreach (var kvp in a)
        {
            if (!b.TryGetValue(kvp.Key, out var value) || !EqualityComparer<TValue>.Default.Equals(kvp.Value, value))
                return false;
        }
        return true;
    }

    private static int ComputeHashCode(Dictionary<TKey, TValue>? dictionary)
    {
        if (dictionary is null) return 0;

        var hash = new HashCode();
        foreach (var kvp in dictionary.OrderBy(k => k.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }
        return hash.ToHashCode();
    }

    private static Dictionary<TKey, TValue>? CreateSnapshot(Dictionary<TKey, TValue>? dictionary)
        => dictionary is null ? null : new Dictionary<TKey, TValue>(dictionary);
}
