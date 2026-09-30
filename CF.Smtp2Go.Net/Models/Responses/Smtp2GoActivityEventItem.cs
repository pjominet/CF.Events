using System.Text.Json.Serialization;

namespace CF.Smtp2Go.Net.Models.Responses;

public class Smtp2GoActivityEventItem
{
    [JsonPropertyName("email_id")]
    public string? EmailId { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("sender")]
    public string? Sender { get; set; }

    [JsonPropertyName("sender_full")]
    public string? SenderFull { get; set; }

    [JsonPropertyName("recipient")]
    public string? Recipient { get; set; }

    [JsonPropertyName("to")]
    public string? To { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("smtp_response")]
    public string? SmtpResponse { get; set; }

    [JsonPropertyName("host")]
    public string? Host { get; set; }

    [JsonPropertyName("outbound_ip")]
    public string? OutboundIp { get; set; }

    [JsonPropertyName("byte_size")]
    public long? ByteSize { get; set; }

    [JsonPropertyName("subaccount_name")]
    public string? SubaccountName { get; set; }

    [JsonPropertyName("subaccount_id")]
    public object? SubaccountId { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; set; }

    [JsonPropertyName("srchost")]
    public string? SrcHost { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("link_url")]
    public string? LinkUrl { get; set; }

    [JsonPropertyName("geoip_city")]
    public string? GeoIpCity { get; set; }

    [JsonPropertyName("geoip_country")]
    public string? GeoIpCountry { get; set; }

    [JsonIgnore]
    public string EffectiveSender => From ?? Sender ?? SenderFull ?? string.Empty;

    [JsonIgnore]
    public string EffectiveRecipient => Recipient ?? To ?? string.Empty;

    [JsonIgnore]
    public string? EffectiveUrl => Url ?? LinkUrl;
}
