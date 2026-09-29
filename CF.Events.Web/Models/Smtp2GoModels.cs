using System.Text.Json;
using System.Text.Json.Serialization;

namespace CF.Events.Web.Models;

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

public class Smtp2GoBulkEmailRequest
{
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

public class Smtp2GoActivitySearchRequest
{
    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }

    [JsonPropertyName("search")]
    public string? Search { get; set; }

    [JsonPropertyName("search_subject")]
    public string? SearchSubject { get; set; }

    [JsonPropertyName("search_sender")]
    public string? SearchSender { get; set; }

    [JsonPropertyName("search_recipient")]
    public string? SearchRecipient { get; set; }

    [JsonPropertyName("search_usernames")]
    public List<string>? SearchUsernames { get; set; }

    [JsonPropertyName("subaccounts")]
    public List<string>? Subaccounts { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; set; }

    [JsonPropertyName("only_latest")]
    public bool? OnlyLatest { get; set; }

    [JsonPropertyName("only_latest_by_sent")]
    public bool? OnlyLatestBySent { get; set; }

    [JsonPropertyName("event_types")]
    public List<string>? EventTypes { get; set; }

    [JsonPropertyName("include_headers")]
    public bool? IncludeHeaders { get; set; }

    [JsonPropertyName("custom_headers")]
    public List<string>? CustomHeaders { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }
}

public class Smtp2GoActivitySearchData
{
    [JsonPropertyName("events")]
    public List<Smtp2GoActivityEventItem> Events { get; set; } = [];

    [JsonPropertyName("total_events")]
    public int TotalEvents { get; set; }

    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; set; }
}

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

public class Smtp2GoTemplateSearchData
{
    [JsonPropertyName("continue_token")]
    public string? ContinueToken { get; set; }

    [JsonPropertyName("templates")]
    public List<Smtp2GoTemplateItem> Templates { get; set; } = [];

    [JsonPropertyName("total_count")]
    public int? TotalCount { get; set; }
}

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
