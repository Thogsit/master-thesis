using System.Collections.Immutable;

namespace SealedFga;

public static class Settings {
    public const string PackageNamespace = "SealedFga";
    public const string UtilNamespace = PackageNamespace + ".Util";
    public const string FgaNamespace = PackageNamespace + ".Fga";
    public const string AuthModelNamespace = PackageNamespace + ".AuthModel";

    public static readonly ImmutableArray<string> HttpEndpointAttributeFullNames = [
        "Microsoft.AspNetCore.Mvc.HttpGetAttribute",
        "Microsoft.AspNetCore.Mvc.HttpPostAttribute",
        "Microsoft.AspNetCore.Mvc.HttpPutAttribute",
        "Microsoft.AspNetCore.Mvc.HttpDeleteAttribute",
        "Microsoft.AspNetCore.Mvc.HttpPatchAttribute",
    ];
}
