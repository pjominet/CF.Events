using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CF.Events.Web.Infrastructure.ModelBinders;

public class JsonModelBinder : IModelBinder
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        var value = valueProviderResult.FirstValue;
        if (string.IsNullOrWhiteSpace(value))
            return Task.CompletedTask;

        try
        {
            var result = JsonSerializer.Deserialize(value, bindingContext.ModelType, _jsonSerializerOptions);
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch (JsonException)
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Invalid JSON format.");
        }

        return Task.CompletedTask;
    }
}
