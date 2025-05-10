using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SealedFga.Sample.FgaAuthorization;

public class FgaEntityModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        System.ArgumentNullException.ThrowIfNull(context);

        return context.BindingInfo.BinderType != typeof(FgaEntityModelBinder) ? null : new FgaEntityModelBinder();
    }
}
