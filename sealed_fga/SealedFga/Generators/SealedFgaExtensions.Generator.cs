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
                ///    Retrieves all SealedFGA ID types from the assembly.
                /// </summary>
                private static IEnumerable<Type> GetSealedFgaIdTypes()
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var idTypes = assembly.GetTypes()
                                          .Where(t => t.GetCustomAttribute<SealedFgaTypeIdAttribute>() is not null);

                    return idTypes;
                }

                /// <summary>
                ///     Configures the EF Core model builder to use the SealedFGA ID types.
                ///     Has to be called from the DbContext's ConfigureConventions method.
                /// </summary>
                public static void ConfigureSealedFga(this ModelConfigurationBuilder configurationBuilder)
                {
                    // Retrieve all SealedFGA ID Types from the assembly
                    var idTypes = GetSealedFgaIdTypes();

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
