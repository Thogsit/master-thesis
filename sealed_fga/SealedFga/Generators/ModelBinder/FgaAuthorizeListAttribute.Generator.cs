using System.Collections.Generic;
using SealedFga.Models;

namespace SealedFga.Generators.ModelBinder;

public static class FgaAuthorizeListAttributeGenerator
{
    public static GeneratedFile Generate() => new(
        $"FgaAuthorizeListAttribute.g.cs",
        $$"""

          [AttributeUsage(AttributeTargets.Parameter)]
          public class FgaAuthorizeListAttribute() : ModelBinderAttribute(typeof(SealedFgaEntityListModelBinder))
          {
              public required string Relation { get; set; }
          }
          """,
        new HashSet<string>([
            "System",
            "Microsoft.AspNetCore.Mvc"
        ]),
        Settings.PackageNamespace + ".ModelBinder"
    );
}