using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators;

public static class SealedFgaExtensionsGenerator {
    public static GeneratedFile Generate()
        => new(
            "SealedFgaExtensions.g.cs",
            """

            /// <summary>
            ///     Contains all extension methods the SealedFga library provides/uses.
            /// </summary>
            public static class SealedFgaExtensions
            {
                /// <summary>
                ///    Retrieves all OpenFGA ID types from the assembly.
                /// </summary>
                private static IEnumerable<Type> GetOpenFgaIdTypes()
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var idTypes = assembly.GetTypes()
                                          .Where(t => t.GetCustomAttribute<OpenFgaTypeIdAttribute>() is not null);

                    return idTypes;
                }

                /// <summary>
                ///     Configures the EF Core model builder to use the OpenFGA ID types.
                ///     Has to be called from the DbContext's ConfigureConventions method.
                /// </summary>
                public static void ConfigureSealedFga(this ModelConfigurationBuilder configurationBuilder)
                {
                    // Retrieve all OpenFGA ID Types from the assembly
                    var idTypes = GetOpenFgaIdTypes();

                    // Retrieve contained EF Core ValueConverter classes and register them
                    foreach (var type in idTypes) {
                        var valueConverters = type.GetNestedTypes().Where(t => t.IsSubclassOf(typeof(ValueConverter)));
                        foreach (var valueConverter in valueConverters) {
                            configurationBuilder
                               .Properties(type)
                               .HaveConversion(valueConverter);
                        }
                    }
                }
            }
            """,
            new HashSet<string>([
                    "Microsoft.EntityFrameworkCore",
                    "Microsoft.EntityFrameworkCore.Storage.ValueConversion",
                    "SealedFga.Attributes",
                    "System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Reflection",
                ]
            )
        );
}
