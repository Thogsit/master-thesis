using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators.ModelBinder;

public static class SealedFgaModelBinderProviderGenerator {
    public static GeneratedFile Generate()
        => new(
            "SealedFgaModelBinderProvider.g.cs",
            """

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
            """,
            new HashSet<string>([
                    "Microsoft.AspNetCore.Mvc.ModelBinding",
                ]
            ),
            Settings.ModelBindingNamespace
        );
}
