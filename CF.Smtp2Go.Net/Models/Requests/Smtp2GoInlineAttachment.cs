using System.Text.Json.Serialization;

namespace CF.Smtp2Go.Net.Models.Requests;

public class Smtp2GoInlineAttachment
{
    [JsonPropertyName("filename")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileblob")]
    public string? FileBlob { get; set; }

    [JsonPropertyName("mimetype")]
    public string? MimeType { get; set; }
}
