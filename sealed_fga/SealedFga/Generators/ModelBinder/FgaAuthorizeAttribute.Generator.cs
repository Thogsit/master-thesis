using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators.ModelBinder;

public static class FgaAuthorizeAttributeGenerator {
    public static GeneratedFile Generate()
        => new(
            "FgaAuthorizeAttribute.g.cs",
            """

            /// <summary>
            ///     Specifies that the parameter should be bound using FGA authorization for a specific relation.
            /// </summary>
            [AttributeUsage(AttributeTargets.Parameter)]
            public class FgaAuthorizeAttribute() : ModelBinderAttribute(typeof(SealedFgaEntityModelBinder))
            {
                /// <summary>
                ///     The relation to check for authorization.
                /// </summary>
                public required string Relation { get; set; }

                /// <summary>
                ///     The name of the parameter to use for authorization.
                /// </summary>
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
