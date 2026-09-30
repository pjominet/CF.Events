using System.Text.Json.Serialization;

namespace CF.Smtp2Go.Net.Models.Responses;

public class Smtp2GoActivitySearchData
{
    [JsonPropertyName("events")]
    public List<Smtp2GoActivityEventItem> Events { get; set; } = [];

    [JsonPropertyName("total_events")]
    public int TotalEvents { get; set; }

    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; set; }
}
