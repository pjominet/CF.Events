using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CF.Events.Web.Data.Converters;

public class DictionaryConverter<TKey, TValue>() : ValueConverter<Dictionary<TKey, TValue>, string>(
    v => JsonSerializer.Serialize(v),
    v => JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(v) ?? new Dictionary<TKey, TValue>())
where TKey : struct, Enum;
