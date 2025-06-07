using System.Collections.Generic;
using System.Text;
using SealedFga.Models;

namespace SealedFga.Util;

public static class GeneratorUtil {
#pragma warning disable CS8524
    public static string GetCsharpTypeByOpenFgaIdType(OpenFgaTypeIdType type)
        => type switch {
            OpenFgaTypeIdType.String => "string",
            OpenFgaTypeIdType.Guid => "Guid",
        };
#pragma warning restore CS8524

    public static string BuildLinesWithIndent(IEnumerable<string> lines, int indent) {
        var asStr = new StringBuilder();
        foreach (var line in lines) {
            // Only add indent if it's not the first line
            if (asStr.Length > 0) asStr.Append(new string(' ', indent));

            asStr.AppendLine(line);
        }

        return asStr.ToString();
    }
}
