using System.Text.Json;
using System.Text.Json.Serialization;

namespace CF.Smtp2Go.Net.Models.Responses;

public class Smtp2GoApiResponse
{
    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}
