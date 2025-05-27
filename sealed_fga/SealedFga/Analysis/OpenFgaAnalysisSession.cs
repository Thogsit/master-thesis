using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;

// We need this to be able to use the "internal" GlobalFlowStateAnalysis
[assembly: IgnoresAccessChecksTo("Microsoft.CodeAnalysis.AnalyzerUtilities")]

namespace SealedFga.Analysis;

public class OpenFgaAnalysisSession(
    DiagnosticDescriptor rule,
    INamedTypeSymbol implementedByAttributeSymbol,
    ImmutableHashSet<INamedTypeSymbol> httpEndpointAttributeSymbols
)
{
    private readonly HashSet<INamedTypeSymbol> _allRelevantClassSymbols = new(SymbolEqualityComparer.Default);

    private readonly Dictionary<INamedTypeSymbol, AttributeData> _implementedByAttributeByInterface =
        new(SymbolEqualityComparer.Default);

    private readonly List<HttpEndpointAnalysisContext> _httpEndpointMethodContexts = [];

    public void OnSemanticModelDataGathering(SemanticModelAnalysisContext context)
    {
        var root = context.SemanticModel.SyntaxTree.GetRoot();

        // Parse all classes that could potentially implement our interfaces
        foreach (var classDeclarationSyntax in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var classSymbol = (INamedTypeSymbol?)context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
            if (classSymbol is null)
            {
                continue;
            }

            // Abstract classes can't be the "final" implementer of our interfaces -> skip
            if (classSymbol.IsAbstract)
            {
                continue;
            }

            _allRelevantClassSymbols.Add(classSymbol);
        }

        // Parse all interfaces with our "ImplementedBy" attribute
        foreach (var interfaceDeclarationSyntax in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
        {
            var interfaceSymbol =
                (INamedTypeSymbol?)context.SemanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);

            // Check if the interface has the "ImplementedBy" attribute
            var implementedByAttributeData = interfaceSymbol?.GetAttributes().FirstOrDefault(attr =>
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass, implementedByAttributeSymbol)
            );
            if (implementedByAttributeData is null)
            {
                continue;
            }

            _implementedByAttributeByInterface[interfaceSymbol!] = implementedByAttributeData;
        }

        // Parse all http endpoints for later analysis
        foreach (var methodDeclarationSyntax in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var methodDeclarationSymbol =
                (IMethodSymbol?)context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
            if (methodDeclarationSymbol is null)
            {
                continue;
            }

            // We don't need to analyze methods that are not the direct entry points
            if (methodDeclarationSymbol.IsAbstract)
            {
                continue;
            }

            // Skip non-HTTP methods
            if (!methodDeclarationSymbol.GetAttributes().Any(attr =>
                    attr.AttributeClass is not null && httpEndpointAttributeSymbols.Contains(attr.AttributeClass)))
            {
                continue;
            }

            _httpEndpointMethodContexts.Add(
                new HttpEndpointAnalysisContext(
                    methodDeclarationSymbol,
                    methodDeclarationSyntax,
                    context.SemanticModel
                )
            );
        }
    }

    public void OnCompilationEndRunAnalysis(CompilationAnalysisContext context)
    {
        var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(context.Compilation);

        foreach (var httpEndpointMethodContext in _httpEndpointMethodContexts)
        {
            var cfg = ControlFlowGraph.Create(
                httpEndpointMethodContext.MethodSyntax,
                httpEndpointMethodContext.MethodSemanticModel
            );

            // Should not happen?
            if (cfg == null)
            {
                continue;
            }

            // Extract auth data from annotated parameters
            foreach (var httpParamSymbol in httpEndpointMethodContext.MethodSymbol.Parameters)
            {
                foreach (var attrData in httpParamSymbol.GetAttributes())
                {
                    //if (attrData.AttributeClass is null || attrData.)
                }
            }

            var analysisResult = GlobalFlowStateAnalysis.TryGetOrComputeResult(
                cfg,
                httpEndpointMethodContext.MethodSymbol,
                (ctx) => new OpenFgaDataFlowVisitor(
                    ctx,
                    new Dictionary<string, HashSet<string>>()
                ),
                wellKnownTypeProvider,
                context.Options,
                rule,
                true, // performValueContentAnalysis
                false, // pessimisticAnalysis
                out var valueContentAnalysisResult,
                InterproceduralAnalysisKind.ContextSensitive
            );
            var x = 42; // Used as a debug breakpoint for analysisResult inspection
        }
    }
}