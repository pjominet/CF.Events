using System.Text.Json;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using CF.Smtp2Go.Net;
using CF.Smtp2Go.Net.Models.Requests;
using CF.Smtp2Go.Net.Models.Responses;
using Microsoft.Extensions.Caching.Memory;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Services;

public interface IEmailTemplateService
{
    Task<IReadOnlyList<EmailTemplate>> GetEmailTemplatesAsync(string[] allowedTags, bool forceRefresh = false, CancellationToken ctx = default);
}

public class EmailTemplateService(
    ISmtp2GoClient smtp2GoClient,
    IMemoryCache memoryCache,
    ILogger<EmailTemplateService> logger) : IEmailTemplateService
{
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromHours(1);

    public async Task<IReadOnlyList<EmailTemplate>> GetEmailTemplatesAsync(string[] allowedTags, bool forceRefresh = false, CancellationToken ctx = default)
    {
        var templates = await GetTemplatesAsync(allowedTags ,forceRefresh, ctx);
        return templates
            .Select(t => new EmailTemplate(t.Id, $"{t.Name} ({t.Id})"))
            .ToList()
            .AsReadOnly();
    }

    private async Task<IReadOnlyList<Smtp2GoTemplateItem>> GetTemplatesAsync(string[] allowedTags, bool forceRefresh = false, CancellationToken ctx = default)
    {
        if (!forceRefresh && memoryCache.TryGetValue<IReadOnlyList<Smtp2GoTemplateItem>>(CacheKeys.EmailTemplates, out var cached) && cached is not null)
            return cached;

        try
        {
            var allTemplates = new List<Smtp2GoTemplateItem>();
            string? continueToken = null;
            var page = 0;
            const int maxPages = 10;

            do
            {
                var request = new Smtp2GoTemplateSearchRequest
                {
                    Limit = 100,
                    SortDirection = "asc",
                    ContinueToken = continueToken
                };

                var response = await smtp2GoClient.SearchTemplatesAsync(request, ctx);
                var searchData = DeserializeTemplateData(response.Data);

                if (searchData?.Templates is { Count: > 0 })
                    allTemplates.AddRange(searchData.Templates);

                continueToken = searchData?.ContinueToken;
                page++;
            } while (continueToken.HasValue() && page < maxPages);

            var result = allTemplates
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .Where(t => t.Tags?.Any(tag => allowedTags.Contains(tag)) ?? true)
                .ToList()
                .AsReadOnly();

            memoryCache.Set(CacheKeys.EmailTemplates, result, DefaultCacheDuration);
            logger.LogInformation("Fetched and cached {Count} email templates from SMTP2GO", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch email templates from SMTP2GO");
            if (memoryCache.TryGetValue<IReadOnlyList<Smtp2GoTemplateItem>>(CacheKeys.EmailTemplates, out var fallback) && fallback is not null)
                return fallback;

            return [];
        }
    }

    private static Smtp2GoTemplateSearchData? DeserializeTemplateData(JsonElement data)
    {
        if (data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        var json = data.GetRawText();
        return JsonSerializer.Deserialize<Smtp2GoTemplateSearchData>(json);
    }
}
