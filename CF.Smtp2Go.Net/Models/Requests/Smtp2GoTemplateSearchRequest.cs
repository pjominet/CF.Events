using System.Text.Json.Serialization;

namespace CF.Smtp2Go.Net.Models.Requests;

public class Smtp2GoTemplateSearchRequest
{
    [JsonPropertyName("search")]
    public string? Search { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("exact_match")]
    public bool? ExactMatch { get; set; }

    [JsonPropertyName("sort_direction")]
    public string? SortDirection { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; set; }
}
