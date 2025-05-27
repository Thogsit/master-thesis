using System.Collections.Immutable;

namespace SealedFga;

public static class Settings
{
    public const string PackageNamespace = "SealedFga";
    public const string ModelBindingNamespace = PackageNamespace + ".ModelBinding";

    public static readonly ImmutableArray<string> HttpEndpointAttributeFullNames = ImmutableArray.Create(
        "Microsoft.AspNetCore.Mvc.HttpGetAttribute",
        "Microsoft.AspNetCore.Mvc.HttpPostAttribute",
        "Microsoft.AspNetCore.Mvc.HttpPutAttribute",
        "Microsoft.AspNetCore.Mvc.HttpDeleteAttribute",
        "Microsoft.AspNetCore.Mvc.HttpPatchAttribute");
}
