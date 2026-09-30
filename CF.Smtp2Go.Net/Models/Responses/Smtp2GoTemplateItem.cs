using System.Text.Json.Serialization;

namespace CF.Smtp2Go.Net.Models.Responses;

public class Smtp2GoTemplateItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("last_updated")]
    public string? LastUpdated { get; set; }
}
