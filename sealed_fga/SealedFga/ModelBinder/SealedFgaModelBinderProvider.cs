using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SealedFga.ModelBinder;

/// <summary>
///     Provides FGA model binders.
/// </summary>
/// <typeparam name="TDb">The database context type.</typeparam>
public class SealedFgaModelBinderProvider<TDb> : IModelBinderProvider {
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
        => context.BindingInfo.BinderType switch {
            { } t when t == typeof(SealedFgaEntityModelBinder) => new SealedFgaEntityModelBinder(typeof(TDb)),
            { } t when t == typeof(SealedFgaEntityListModelBinder) => new SealedFgaEntityListModelBinder(typeof(TDb)),
            _ => null,
        };
}
