using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CF.Events.Web.Data.Converters;

public class CollectionConverter<T>() : ValueConverter<T[], string>(
    v => string.Join('/', v),
    v => v.Split('/', StringSplitOptions.RemoveEmptyEntries)
        .Select(Enum.Parse<T>)
        .ToArray())
    where T : struct, Enum;
