using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators.ModelBinder;

public static class SealedFgaModelBinderProviderGenerator {
    public static GeneratedFile Generate()
        => new(
            "SealedFgaModelBinderProvider.g.cs",
            """

            /// <summary>
            ///     Provides FGA model binders.
            /// </summary>
            /// <typeparam name="TDb">The database context type.</typeparam>
            public class SealedFgaModelBinderProvider<TDb> : IModelBinderProvider
            {
                /// <inheritdoc />
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
            """,
            new HashSet<string>([
                    "Microsoft.AspNetCore.Mvc.ModelBinding",
                ]
            ),
            Settings.ModelBindingNamespace
        );
}
