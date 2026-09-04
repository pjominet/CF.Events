using CF.Events.Web.Infrastructure.ModelBinders;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace CF.Events.Web.Infrastructure.Providers;

public class SanitizedStringModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
        => context.Metadata.ModelType == typeof(string) ? new BinderTypeModelBinder(typeof(SanitizedStringModelBinder)) : null;
}
