using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CF.Events.Web.Data.Converters;

public class EnumArrayConverter<TEnum>() : ValueConverter<TEnum[], string>(
    v => string.Join('/', v),
    v => v.Split('/', StringSplitOptions.RemoveEmptyEntries)
        .Select(Enum.Parse<TEnum>)
        .ToArray())
    where TEnum : struct, Enum;
