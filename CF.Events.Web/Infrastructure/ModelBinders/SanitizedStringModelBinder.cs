using System.Net;
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
            bindingContext.Result = ModelBindingResult.Success(value);
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

        // HtmlSanitizer encodes special characters like & to &amp; (because of AngleSharp's parsing algorithm)
        // Since using this globally, decode them back to avoid double-encoding in Razor views and display issues.
        var decodedValue = WebUtility.HtmlDecode(sanitizedValue);

        bindingContext.Result = ModelBindingResult.Success(decodedValue.Trim());
        return Task.CompletedTask;
    }
}
