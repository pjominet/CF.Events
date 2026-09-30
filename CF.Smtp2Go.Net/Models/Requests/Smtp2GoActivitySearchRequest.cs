using System.Text.Json.Serialization;

namespace CF.Smtp2Go.Net.Models.Requests;

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
