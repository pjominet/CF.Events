using System.Text.Json;
using System.Text.Json.Serialization;

namespace CF.Events.Web.Models;

public class Smtp2GoEmailRequest
{
    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

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

public class Smtp2GoBulkEmailRequest
{
    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("emails")]
    public List<Smtp2GoEmailRequestItem> Emails { get; set; } = [];
}

public class Smtp2GoEmailRequestItem
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

public class Smtp2GoInlineAttachment
{
    [JsonPropertyName("filename")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileblob")]
    public string? FileBlob { get; set; }

    [JsonPropertyName("mimetype")]
    public string? MimeType { get; set; }
}

public class Smtp2GoApiResponse
{
    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}
