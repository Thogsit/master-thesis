using Microsoft.CodeAnalysis;

namespace SealedFga.Analysis;

public static class SealedFgaDiagnosticRules {
    public static readonly DiagnosticDescriptor FoundContextRule = new(
        "SFGA003",
        "Found context",
        "Found context",
        "Usage",
        DiagnosticSeverity.Warning,
        false,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]
    );

    public static readonly DiagnosticDescriptor PossiblyMisingImplementedByRule = new(
        "SFGA004",
        "Possibly missing ImplementedBy attribute",
        "Possibly missing ImplementedBy attribute on interface {0}",
        "Usage",
        DiagnosticSeverity.Warning,
        true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]
    );
}
