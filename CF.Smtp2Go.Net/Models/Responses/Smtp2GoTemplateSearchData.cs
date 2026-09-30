using System.Text.Json.Serialization;

namespace CF.Smtp2Go.Net.Models.Responses;

public class Smtp2GoTemplateSearchData
{
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; set; }

    [JsonPropertyName("templates")]
    public List<Smtp2GoTemplateItem> Templates { get; set; } = [];

    [JsonPropertyName("total_count")]
    public int? TotalCount { get; set; }
}
