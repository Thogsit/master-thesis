using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SealedFga.Models;
using SealedFga.Util;

namespace SealedFga.Generators;

public static class SealedFgaInitGenerator {
    public static GeneratedFile Generate(ImmutableArray<IdClassToGenerateData> idClasses)
        => new(
            "SealedFgaInit.g.cs",
            $$"""
              /// <summary>
              ///     SealedFga static class for initialization and registration of OpenFGA type IDs.
              /// </summary>
              public static class SealedFgaInit {
                  /// <summary>
                  ///     Initializes the SealedFga library by registering all OpenFGA type IDs.
                  /// </summary>
                  public static void Initialize() {
                      // Register all OpenFGA type IDs.
                      {{GetIdUtilInitializationBlock(idClasses)}}
                  }
              }
              """,
            new HashSet<string>([
                    Settings.UtilNamespace,
                ]
            )
        );

    private static string GetIdUtilInitializationBlock(ImmutableArray<IdClassToGenerateData> idClasses) {
        var idUtilInitializations = idClasses
           .SelectMany(idClass => {
                    var fullClassName = $"{idClass.ClassNamespace}.{idClass.ClassName}";
                    return new List<string>([
                            $"IdUtil.RegisterIdType(typeof({fullClassName}), \"{idClass.TypeName}\");",
                            $"IdUtil.RegisterIdTypeParseMethod(typeof({fullClassName}), {fullClassName}.Parse);",
                        ]
                    );
                }
            );

        return GeneratorUtil.BuildLinesWithIndent(
            idUtilInitializations,
            8
        );
    }
}
