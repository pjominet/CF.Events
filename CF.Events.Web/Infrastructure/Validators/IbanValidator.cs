using System.Text.RegularExpressions;

namespace CF.Events.Web.Infrastructure.Validators;

public static partial class IbanValidator
{
    private static readonly Regex IbanFormatRegex = IbanRegex();

    private static readonly Dictionary<string, int> CountryIbanLengths = new()
    {
        { "AL", 28 }, { "AD", 24 }, { "AT", 20 }, { "AZ", 28 }, { "BH", 22 }, { "BE", 16 }, { "BA", 20 }, { "BR", 29 },
        { "BG", 22 }, { "CR", 22 }, { "HR", 21 }, { "CY", 28 }, { "CZ", 24 }, { "DK", 18 }, { "DO", 28 }, { "EE", 20 },
        { "FO", 18 }, { "FI", 18 }, { "FR", 27 }, { "GE", 22 }, { "DE", 22 }, { "GI", 23 }, { "GR", 27 }, { "GL", 18 },
        { "GT", 28 }, { "HU", 28 }, { "IS", 26 }, { "IE", 22 }, { "IL", 23 }, { "IT", 27 }, { "JO", 30 }, { "KZ", 20 },
        { "KW", 30 }, { "LV", 21 }, { "LB", 28 }, { "LI", 21 }, { "LT", 20 }, { "LU", 20 }, { "MK", 19 }, { "MT", 31 },
        { "MR", 27 }, { "MU", 30 }, { "MD", 24 }, { "MC", 27 }, { "ME", 22 }, { "NL", 18 }, { "NO", 15 }, { "PK", 24 },
        { "PS", 29 }, { "PL", 28 }, { "PT", 25 }, { "QA", 29 }, { "RO", 24 }, { "LC", 32 }, { "SM", 27 }, { "ST", 25 },
        { "SA", 24 }, { "RS", 22 }, { "SC", 31 }, { "SK", 24 }, { "SI", 19 }, { "ES", 24 }, { "SE", 24 }, { "CH", 21 },
        { "TL", 23 }, { "TN", 24 }, { "TR", 26 }, { "UA", 29 }, { "AE", 23 }, { "GB", 22 }, { "VG", 24 }, { "XK", 20 }
    };

    public static bool IsValid(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return false;

        // 1. Remove spaces and convert to uppercase
        var cleanedIban = iban.Replace(" ", "").ToUpperInvariant();

        // 2. Basic format check
        if (!IbanFormatRegex.IsMatch(cleanedIban))
            return false;

        // 3. Country-specific length check
        var countryCode = cleanedIban[..2];
        if (CountryIbanLengths.TryGetValue(countryCode, out var expectedLength))
        {
            if (cleanedIban.Length != expectedLength)
                return false;
        }
        // else if country not in list, fallback to standard ISO length range (15-34)
        // which is already checked by regex: 15-34 range (11-30 + 4 prefix)

        // 4. Move the first four characters to the end
        var rearranged = cleanedIban[4..] + cleanedIban[..4];

        // 5. Replace letters with numbers and compute MOD 97
        return Mod97(rearranged) == 1;
    }

    private static int Mod97(string rearrangedIban)
    {
        var remainder = 0;
        foreach (var c in rearrangedIban)
        {
            int value;
            if (char.IsDigit(c))
            {
                value = c - '0';
                remainder = (remainder * 10 + value) % 97;
            }
            else
            {
                // A=10, B=11, ..., Z=35
                value = c - 'A' + 10;
                // Two digits, so two shifts
                // e.g., 'A' (10): remainder = (remainder * 10 + 1) % 97; remainder = (remainder * 10 + 0) % 97;
                remainder = (remainder * 10 + (value / 10)) % 97;
                remainder = (remainder * 10 + (value % 10)) % 97;
            }
        }

        return remainder;
    }

    [GeneratedRegex("^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$", RegexOptions.Compiled)]
    private static partial Regex IbanRegex();
}
