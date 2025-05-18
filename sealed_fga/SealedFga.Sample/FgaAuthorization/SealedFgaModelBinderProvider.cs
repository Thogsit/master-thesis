using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SealedFga.Sample.FgaAuthorization;

public class SealedFgaModelBinderProvider<TDb> : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        System.ArgumentNullException.ThrowIfNull(context);

        return context.BindingInfo.BinderType switch
        {
            { } t when t == typeof(SealedFgaEntityModelBinder) => new SealedFgaEntityModelBinder(typeof(TDb)),
            { } t when t == typeof(SealedFgaEntityListModelBinder) => new SealedFgaEntityListModelBinder(typeof(TDb)),
            _ => null,
        };
    }
}