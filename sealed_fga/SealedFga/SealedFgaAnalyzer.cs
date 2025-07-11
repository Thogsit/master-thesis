using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SealedFga.Analysis;
using SealedFga.Attributes;

namespace SealedFga;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SealedFgaAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [
        SealedFgaDiagnosticRules.FoundContextRule,
        SealedFgaDiagnosticRules.PossiblyMisingImplementedByRule,
    ];

    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution(); // TODO: Put back in when done with debugging
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationStartContext => {
                // Find the "ImplementedBy" attribute symbol to be used for dependency injection on control flow analysis
                var implementedByAttributeSymbol = compilationStartContext.Compilation.GetTypeByMetadataName(
                    typeof(ImplementedByAttribute).FullName!
                )!;
                var fgaAuthorizeAttributeSymbol = compilationStartContext.Compilation.GetTypeByMetadataName(
                    typeof(FgaAuthorizeAttribute).FullName!
                )!;
                var fgaAuthorizeListAttributeSymbol = compilationStartContext.Compilation.GetTypeByMetadataName(
                    typeof(FgaAuthorizeListAttribute).FullName!
                )!;
                var httpEndpointAttributes =
                    Settings.HttpEndpointAttributeFullNames.Select(name =>
                                 compilationStartContext.Compilation.GetTypeByMetadataName(name)!
                             )
                            .ToImmutableHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

                // Register the analysis sessions' handlers
                var analysisSession = new SealedFgaAnalysisSession(
                    implementedByAttributeSymbol,
                    fgaAuthorizeAttributeSymbol,
                    fgaAuthorizeListAttributeSymbol,
                    httpEndpointAttributes
                );
                compilationStartContext.RegisterSemanticModelAction(analysisSession.OnSemanticModelDataGathering);

                // Register handler for the real analysis after all data has been gathered
                compilationStartContext.RegisterCompilationEndAction(analysisSession.OnCompilationEndRunAnalysis);
            }
        );
    }
}
