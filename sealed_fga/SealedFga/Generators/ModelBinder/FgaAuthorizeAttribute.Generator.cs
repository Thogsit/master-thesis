using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators.ModelBinder;

public static class FgaAuthorizeAttributeGenerator {
    public static GeneratedFile Generate()
        => new(
            "FgaAuthorizeAttribute.g.cs",
            """

            [AttributeUsage(AttributeTargets.Parameter)]
            public class FgaAuthorizeAttribute() : ModelBinderAttribute(typeof(SealedFgaEntityModelBinder))
            {
                public required string Relation { get; set; }
                public required string ParameterName { get; set; }
            }
            """,
            new HashSet<string>([
                    "System",
                    "Microsoft.AspNetCore.Mvc",
                ]
            ),
            Settings.ModelBindingNamespace
        );
}
