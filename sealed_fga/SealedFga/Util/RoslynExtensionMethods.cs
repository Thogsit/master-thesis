using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SealedFga.Util;

public static class RoslynExtensionMethods
{
    public static string FullName(this INamespaceSymbol namespaceSymbol)
    {
        var parts = new Stack<string>();
        var current = namespaceSymbol;

        while (current is { IsGlobalNamespace: false })
        {
            parts.Push(current.Name);
            current = current.ContainingNamespace;
        }

        return string.Join(".", parts);
    }
}
