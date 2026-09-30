using System.Text.Json.Serialization;

namespace CF.Smtp2Go.Net.Models.Requests;

public class Smtp2GoEmailRequest
{
    [JsonPropertyName("template_id")]
    public string? TemplateId { get; set; }

    [JsonPropertyName("sender")]
    public string? Sender { get; set; }

    [JsonPropertyName("to")]
    public List<string> To { get; set; } = [];

    [JsonPropertyName("template_data")]
    public IDictionary<string, string>? TemplateData { get; set; }

    [JsonPropertyName("inlines")]
    public List<Smtp2GoInlineAttachment>? Inlines { get; set; }
}
