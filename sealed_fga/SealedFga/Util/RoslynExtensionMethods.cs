using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SealedFga.Util;

/// <summary>
///     Provides extension methods for working with Roslyn symbols and the Roslyn API.
/// </summary>
public static class RoslynExtensionMethods {
    /// <summary>
    ///     Retrieves the full name of a namespace, including its parent namespaces.
    /// </summary>
    /// <param name="namespaceSymbol">The namespace symbol for which the full name is being calculated.</param>
    /// <returns>The fully qualified name of the namespace, with each level separated by a dot.</returns>
    public static string FullName(this INamespaceSymbol namespaceSymbol) {
        var parts = new Stack<string>();
        var current = namespaceSymbol;

        while (current is { IsGlobalNamespace: false }) {
            parts.Push(current.Name);
            current = current.ContainingNamespace;
        }

        return string.Join(".", parts);
    }

    /// <summary>
    ///     Determines if the specified symbol belongs to the current compilation.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="compilation">The compilation against which to verify the symbol's assembly.</param>
    /// <returns>
    ///     True if the symbol's containing assembly matches the assembly of the specified compilation; otherwise, false.
    /// </returns>
    public static bool IsSymbolFromCurrentCompilation(this ISymbol symbol, Compilation compilation) {
        return symbol.ContainingAssembly is not null
               && SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, compilation.Assembly);
    }
}
