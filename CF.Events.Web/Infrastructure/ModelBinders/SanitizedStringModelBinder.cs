using CF.Events.Web.Infrastructure.Extensions;
using Ganss.Xss;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CF.Events.Web.Infrastructure.ModelBinders;

public class SanitizedStringModelBinder(IHtmlSanitizer sanitizer) : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None) return Task.CompletedTask;

        var value = valueProviderResult.FirstValue;
        if (!value.HasValue())
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        // Check if it's likely a JSON string (EditorJS output)
        if (value.IsJson())
        {
            bindingContext.Result = ModelBindingResult.Success(value);
            return Task.CompletedTask;
        }

        // Sanitize the input
        var sanitizedValue = sanitizer.Sanitize(value);

        if (!sanitizedValue.HasValue())
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        // HtmlSanitizer encodes special characters like & to &amp; (because of AngleSharp's parsing algorithm)
        // Apply selective decoding to prevent double-encoding of safe characters
        var safeValue = sanitizedValue
            .Replace("&amp;", "&")
            .Replace("&quot;", "\"")
            .Replace("&#39;", "'")
            .Replace("&apos;", "'")
            .Trim();

        if (safeValue.Length == 0)
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(safeValue);
        return Task.CompletedTask;
    }
}
