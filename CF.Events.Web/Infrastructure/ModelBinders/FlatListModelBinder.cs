using CF.Events.Web.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CF.Events.Web.Infrastructure.ModelBinders;

public class FlatListModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None) return Task.CompletedTask;

        var value = valueProviderResult.FirstValue;
        if (!value.HasValue())
        {
            bindingContext.Result = ModelBindingResult.Success(new List<string>());
            return Task.CompletedTask;
        }

        var list = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();

        bindingContext.Result = ModelBindingResult.Success(list);
        return Task.CompletedTask;
    }
}
