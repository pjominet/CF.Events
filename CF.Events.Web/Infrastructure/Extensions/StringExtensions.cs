using System.Text.Json;
using System.Text.RegularExpressions;

namespace CF.Events.Web.Infrastructure.Extensions;

public static partial class StringExtensions
{
    extension(string @string)
    {
        public bool IsEmail() => EmailRegex().IsMatch(@string);
        public bool IsPhoneNumber() => PhoneRegex().IsMatch(@string);
        public string FormatAsIban() => string.Join(' ', Enumerable.Range(0,
                (int)Math.Ceiling(@string.Replace(" ", "").Length / 4.0)).Select(i => @string.Replace(" ", "")
            .Substring(i * 4, Math.Min(4, @string.Replace(" ", "").Length - i * 4))));

        public IEnumerable<string> ExtractImageFileNamesFromJsonString()
        {
            if (string.IsNullOrWhiteSpace(@string)) return [];

            try
            {
                using var doc = JsonDocument.Parse(@string);
                if (!doc.RootElement.TryGetProperty("blocks", out var blocks) || blocks.ValueKind is not JsonValueKind.Array)
                    return [];

                var fileNames = new List<string>();
                foreach (var block in blocks.EnumerateArray())
                {
                    if (!block.TryGetProperty("type", out var type) || !type.ValueEquals("image")) continue;

                    if (!block.TryGetProperty("data", out var data) ||
                        !data.TryGetProperty("file", out var file) ||
                        !file.TryGetProperty("url", out var urlProperty)) continue;

                    var url = urlProperty.GetString();
                    if (string.IsNullOrWhiteSpace(url)) continue;

                    fileNames.Add(Path.GetFileName(url));
                }
                return fileNames;
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }

    [GeneratedRegex("(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^(00|\+)?(\d{1,3})?[\s.-]?\(?\d{1,4}\)?[\s.-]?\d{3}[\s.-]?\d{3,4}$")]
    private static partial Regex PhoneRegex();
}
