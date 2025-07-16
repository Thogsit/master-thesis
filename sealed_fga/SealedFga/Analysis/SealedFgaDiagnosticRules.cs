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

    public static readonly DiagnosticDescriptor MissingAuthorizationRule = new(
        "SFGA005",
        "Missing authorization check",
        "Missing authorization checks for {0}: {1}",
        "Security",
        DiagnosticSeverity.Warning,
        true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]
    );
}
