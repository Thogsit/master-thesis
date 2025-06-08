using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;
using SealedFga.Models;

// We need this to be able to use the "internal" GlobalFlowStateAnalysis
[assembly: IgnoresAccessChecksTo("Microsoft.CodeAnalysis.AnalyzerUtilities")]

namespace SealedFga.Analysis;

public class OpenFgaAnalysisSession(
    DiagnosticDescriptor rule,
    INamedTypeSymbol implementedByAttributeSymbol,
    INamedTypeSymbol fgaAuthorizeAttributeSymbol,
    INamedTypeSymbol fgaAuthorizeListAttributeSymbol,
    ImmutableHashSet<INamedTypeSymbol> httpEndpointAttributeSymbols
) {
    private readonly List<HttpEndpointAnalysisContext> _httpEndpointMethodContexts = [];

    private readonly Dictionary<INamedTypeSymbol, AttributeData> _implementedByAttributeByInterface =
        new(SymbolEqualityComparer.Default);

    public void OnSemanticModelDataGathering(SemanticModelAnalysisContext context) {
        var root = context.SemanticModel.SyntaxTree.GetRoot();

        // Parse all interfaces with our "ImplementedBy" attribute
        foreach (var interfaceDeclarationSyntax in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()) {
            var interfaceSymbol =
                (INamedTypeSymbol?) context.SemanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);

            // Check if the interface has the "ImplementedBy" attribute
            var implementedByAttributeData = interfaceSymbol?.GetAttributes().FirstOrDefault(attr =>
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass, implementedByAttributeSymbol)
            );
            if (implementedByAttributeData is null) continue;

            _implementedByAttributeByInterface[interfaceSymbol!] = implementedByAttributeData;
        }

        // Parse all http endpoints for later analysis
        foreach (var methodDeclarationSyntax in root.DescendantNodes().OfType<MethodDeclarationSyntax>()) {
            var methodDeclarationSymbol =
                (IMethodSymbol?) context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
            if (methodDeclarationSymbol is null) continue;

            // We don't need to analyze methods that are not the direct entry points
            if (methodDeclarationSymbol.IsAbstract) continue;

            // Skip non-HTTP methods
            if (!methodDeclarationSymbol.GetAttributes().Any(attr =>
                    attr.AttributeClass is not null && httpEndpointAttributeSymbols.Contains(attr.AttributeClass)
                )) {
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

    public void OnCompilationEndRunAnalysis(CompilationAnalysisContext context) {
        var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(context.Compilation);

        // Build Dependency Injection map
        var interfaceRedirects = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var (interfaceSymbol, implByAttr) in _implementedByAttributeByInterface) {
            if (implByAttr.ConstructorArguments[0].Value is not INamedTypeSymbol implementingClassSymbol) continue;

            interfaceRedirects.Add(interfaceSymbol, implementingClassSymbol.OriginalDefinition);
        }

        // Analyze all http endpoints
        foreach (var httpEndpointMethodContext in _httpEndpointMethodContexts) {
            var cfg = ControlFlowGraph.Create(
                httpEndpointMethodContext.MethodSyntax,
                httpEndpointMethodContext.MethodSemanticModel
            );

            // Should not happen?
            if (cfg == null) continue;

            // Extract auth data from annotated parameters
            var checkedPermissionsByEntityVar = new CheckedPermissionsByEntityVarDict();
            foreach (var httpParamSymbol in httpEndpointMethodContext.MethodSymbol.Parameters)
            foreach (var attrData in httpParamSymbol.GetAttributes()) {
                if (attrData.AttributeClass is null) continue;

                // [FgaAuthorize(Relation = "...", ParameterName = "...")]
                if (SymbolEqualityComparer.Default.Equals(attrData.AttributeClass, fgaAuthorizeAttributeSymbol)) {
                    string? relParam = null;
                    string? paramNameParam = null;
                    foreach (var (paramName, paramVal) in attrData.NamedArguments) {
                        switch (paramName) {
                            case "Relation":
                                relParam = paramVal.Value as string;
                                break;
                            case "ParameterName":
                                paramNameParam = paramVal.Value as string;
                                break;
                        }
                    }

                    if (relParam is not null && paramNameParam is not null) {
                        // ID parameter, e.g. "SecretEntityId secretId"
                        checkedPermissionsByEntityVar.AddPermission(paramNameParam, relParam);
                        // Entity parameter, e.g. "SecretEntity secret"
                        checkedPermissionsByEntityVar.AddPermission(httpParamSymbol.Name, relParam);
                    }
                }

                // [FgaAuthorizeList(Relation = "...")]
                if (SymbolEqualityComparer.Default.Equals(attrData.AttributeClass, fgaAuthorizeListAttributeSymbol)) {
                    if (attrData.NamedArguments.Length > 0) {
                        var relParam = (string) attrData.NamedArguments[0].Value.Value!;
                        checkedPermissionsByEntityVar.AddPermission(httpParamSymbol.Name, relParam);
                    }
                }
            }

            _ = GlobalFlowStateAnalysis.TryGetOrComputeResult(
                cfg,
                httpEndpointMethodContext.MethodSymbol,
                ctx => new OpenFgaDataFlowVisitor(
                    ctx,
                    interfaceRedirects,
                    checkedPermissionsByEntityVar,
                    context,
                    rule
                ),
                wellKnownTypeProvider,
                context.Options,
                rule,
                true, // performValueContentAnalysis
                false, // pessimisticAnalysis
                out _,
                InterproceduralAnalysisKind.ContextSensitive
            );
        }
    }
}
