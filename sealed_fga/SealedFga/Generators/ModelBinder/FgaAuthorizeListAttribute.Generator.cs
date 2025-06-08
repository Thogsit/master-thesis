using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators.ModelBinder;

public static class FgaAuthorizeListAttributeGenerator {
    public static GeneratedFile Generate()
        => new(
            "FgaAuthorizeListAttribute.g.cs",
            """

            /// <summary>
            ///     Specifies that the parameter should be bound as a list using FGA authorization for a specific relation.
            /// </summary>
            [AttributeUsage(AttributeTargets.Parameter)]
            public class FgaAuthorizeListAttribute() : ModelBinderAttribute(typeof(SealedFgaEntityListModelBinder))
            {
                /// <summary>
                ///     The relation to check for authorization.
                /// </summary>
                public required string Relation { get; set; }
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
