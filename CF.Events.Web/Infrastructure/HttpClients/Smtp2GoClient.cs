using System.Text.Json;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;

namespace CF.Events.Web.Infrastructure.HttpClients;

public interface ISmtp2GoClient
{
    Task<Smtp2GoApiResponse> SendTemplatedEmailAsync(Smtp2GoEmailRequest request, CancellationToken ctx = default);
    Task<Smtp2GoApiResponse> SendBulkTemplatedEmailsAsync(Smtp2GoBulkEmailRequest request, CancellationToken ctx = default);
    Task<Smtp2GoApiResponse> SearchActivityAsync(Smtp2GoActivitySearchRequest request, CancellationToken ctx = default);
    Task<Smtp2GoApiResponse> SearchTemplatesAsync(Smtp2GoTemplateSearchRequest request, CancellationToken ctx = default);
}

public class Smtp2GoClient(HttpClient httpClient) : ISmtp2GoClient
{
    public async Task<Smtp2GoApiResponse> SendTemplatedEmailAsync(Smtp2GoEmailRequest request, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync("email/send", request, ctx);
        return await ProcessResponse(response, ctx);
    }

    public async Task<Smtp2GoApiResponse> SendBulkTemplatedEmailsAsync(Smtp2GoBulkEmailRequest request, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync("email/batch", request, ctx);
        return await ProcessResponse(response, ctx);
    }

    public async Task<Smtp2GoApiResponse> SearchActivityAsync(Smtp2GoActivitySearchRequest request, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync("activity/search", request, ctx);
        return await ProcessResponse(response, ctx);
    }

    public async Task<Smtp2GoApiResponse> SearchTemplatesAsync(Smtp2GoTemplateSearchRequest request, CancellationToken ctx = default)
    {
        var response = await httpClient.PostAsJsonAsync("template/search", request, ctx);
        return await ProcessResponse(response, ctx);
    }

    private static async Task<Smtp2GoApiResponse> ProcessResponse(HttpResponseMessage response, CancellationToken ctx)
    {
        var result = await response.Content.ReadFromJsonAsync<Smtp2GoApiResponse>(cancellationToken: ctx);
        if (result is null)
            throw new Exception("Failed to deserialize Smtp2Go API response");

        if (response.IsSuccessStatusCode) return result;

        var statusCode = (int)response.StatusCode;
        var errorDetail = statusCode switch
        {
            400 => "The request was unacceptable, due to missing a required parameter.",
            401 => "No valid API key was provided.",
            402 => "The parameters were valid but the request failed.",
            403 => "The API key doesn't have permission to perform the request.",
            404 => "The requested resource doesn't exist.",
            _ => HandleErrorResponse(result)
        };

        throw new Exception($"Smtp2go API error: {errorDetail} (Request ID: {result.RequestId})");
    }

    private static string HandleErrorResponse(Smtp2GoApiResponse result)
    {
        var apiErrorMessage = ExtractErrorMessage(result.Data);

        if (!apiErrorMessage.HasValue())
            apiErrorMessage = "Unknown Smtp2Go API Error";

        if (apiErrorMessage.Contains("rendering template", StringComparison.OrdinalIgnoreCase))
            apiErrorMessage = "The Template ID is invalid or the template contains syntax errors.";

        return apiErrorMessage;
    }

    private static string? ExtractErrorMessage(JsonElement data)
    {
        if (data.ValueKind is not JsonValueKind.Object)
            return data.ValueKind is JsonValueKind.String ? data.GetString() : null;

        data.TryGetProperty("error", out var errorProp);
        data.TryGetProperty("error_code", out var errorCodeProp);

        var error = errorProp.GetString();
        var errorCode = errorCodeProp.GetString();

        if (error.HasValue() && errorCode.HasValue())
            return $"{errorCode}: {error}";

        return error ?? errorCode;
    }
}
