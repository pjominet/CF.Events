using System.Text.Json.Serialization;

namespace CF.Smtp2Go.Net.Models.Requests;

public class Smtp2GoBulkEmailRequest
{
    [JsonPropertyName("emails")]
    public List<Smtp2GoEmailRequestItem> Emails { get; set; } = [];
}
