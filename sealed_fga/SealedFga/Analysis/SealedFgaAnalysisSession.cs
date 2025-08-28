using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

// We need this to be able to use the "internal" GlobalFlowStateAnalysis
[assembly: IgnoresAccessChecksTo("Microsoft.CodeAnalysis.AnalyzerUtilities")]

namespace SealedFga.Analysis;

public class SealedFgaAnalysisSession(
    INamedTypeSymbol implementedByAttributeSymbol,
    INamedTypeSymbol fgaAuthorizeAttributeSymbol,
    INamedTypeSymbol fgaAuthorizeListAttributeSymbol,
    INamedTypeSymbol sealedFgaGuardSymbol,
    ImmutableHashSet<INamedTypeSymbol> httpEndpointAttributeSymbols
) {
    private readonly ConcurrentDictionary<INamedTypeSymbol, AttributeData> _implByAttrByInterface
        = new(SymbolEqualityComparer.Default);

    private readonly ConcurrentDictionary<INamedTypeSymbol, int> _implementerCountByInterface
        = new(SymbolEqualityComparer.Default);

    /// <summary>
    ///     Triggered after a semantic model is built for a syntax tree.
    ///     Gathers everything we need to know for further analysis from it.
    ///     Could be run concurrently by the compiler.
    /// </summary>
    /// <param name="context">The semantic model analysis context.</param>
    public void OnSemanticModelDataGathering(SemanticModelAnalysisContext context) {
        var root = context.SemanticModel.SyntaxTree.GetRoot();

        // Parse all interfaces with our "ImplementedBy" attribute
        foreach (var interfaceDeclarationSyntax in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()) {
            var interfaceSymbol = context.SemanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);

            if (interfaceSymbol is null) {
                continue;
            }

            // Check if the interface has the "ImplementedBy" attribute
            var implementedByAttributeData = interfaceSymbol.GetAttributes().FirstOrDefault(attr =>
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass, implementedByAttributeSymbol)
            );
            if (implementedByAttributeData is null) {
                _implementerCountByInterface[interfaceSymbol] = 0;
            } else {
                _implByAttrByInterface[interfaceSymbol] = implementedByAttributeData;
            }
        }

        // Iterate over all classes and count how many implement each interface that we're tracking
        foreach (var classDeclarationSyntax in root.DescendantNodes().OfType<ClassDeclarationSyntax>()) {
            var classSymbol =
                (INamedTypeSymbol?) ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclarationSyntax);

            if (classSymbol is null) {
                continue;
            }

            foreach (var classInterface in classSymbol.Interfaces) {
                if (_implementerCountByInterface.ContainsKey(classInterface)) {
                    _implementerCountByInterface[classInterface]++;
                }
            }
        }
    }

    /// <summary>
    ///     Triggered after the compilation is done and all semantic models have been built.
    ///     Uses the gathered data to run the actual analysis.
    ///     This is run only once per compilation, so not concurrently.
    ///     This is where we report diagnostics and perform the main analysis.
    /// </summary>
    /// <param name="context">The compilation analysis context.</param>
    public void OnCompilationEndRunAnalysis(CompilationAnalysisContext context) {
        var diagnosticsReporter = new DepInjectionAwareDiagnosticsReporter(context);

        // Build interface redirects
        var interfaceRedirects = new Dictionary<ITypeSymbol, INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var (interfaceSymbol, implByAttr) in _implByAttrByInterface) {
            if (implByAttr.ConstructorArguments[0].Value is not INamedTypeSymbol implementingClassSymbol) {
                continue;
            }

            interfaceRedirects.Add(interfaceSymbol, implementingClassSymbol.OriginalDefinition);
        }

        // Rewrite the syntax trees to resolve dependency injection
        var dependencyInjectedSyntaxTrees = new List<SyntaxTree>();
        foreach (var syntaxTree in context.Compilation.SyntaxTrees) {
            var sourceLocationMapper = new SourceLocationMapper();
            var depInjectedSyntaxRoot = new DepInjectionSyntaxRewriter(
                    interfaceRedirects,
                    sourceLocationMapper,
                    context.Compilation.GetSemanticModel(syntaxTree)
                )
               .Visit(syntaxTree.GetRoot());
            dependencyInjectedSyntaxTrees.Add(
                syntaxTree.WithRootAndOptions(depInjectedSyntaxRoot, syntaxTree.Options)
            );
            diagnosticsReporter.AddFile(
                syntaxTree.FilePath,
                new DepInjectionAwareDiagnosticsReporter.LocationMapData {
                    LocationMapper = sourceLocationMapper,
                    OldSyntaxTree = syntaxTree,
                }
            );
        }

        // (Re-)compile all syntax trees incl. the newly modified ones
        var compilation = CSharpCompilation.Create(
            Guid.NewGuid().ToString(),
            dependencyInjectedSyntaxTrees,
            context.Compilation.References
        );
        var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);

        // Now analyze the modified syntax trees one by one
        var httpEndpointMethodContexts = new List<HttpEndpointAnalysisContext>();
        var requireCheckCalls = new List<RequireCheckAnalysisContext>();
        foreach (var syntaxTree in compilation.SyntaxTrees) {
            var root = syntaxTree.GetRoot();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Parse all http endpoints for later analysis
            foreach (var methodDeclarationSyntax in root.DescendantNodes().OfType<MethodDeclarationSyntax>()) {
                var methodDeclarationSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                if (methodDeclarationSymbol is null) {
                    continue;
                }

                // We don't need to analyze methods that are not the direct entry points
                if (methodDeclarationSymbol.IsAbstract) {
                    continue;
                }

                // Skip non-HTTP methods
                if (!methodDeclarationSymbol.GetAttributes().Any(attr =>
                        attr.AttributeClass is not null && httpEndpointAttributeSymbols.Contains(attr.AttributeClass)
                    )) {
                    continue;
                }

                httpEndpointMethodContexts.Add(
                    new HttpEndpointAnalysisContext(
                        methodDeclarationSymbol,
                        methodDeclarationSyntax,
                        semanticModel
                    )
                );
            }

            // Parse all RequireCheck calls for later analysis
            var requireCheckMethodSymbols = sealedFgaGuardSymbol.GetMembers().OfType<IMethodSymbol>().ToList();
            foreach (var invocationSyntax in root.DescendantNodes().OfType<InvocationExpressionSyntax>()) {
                var methodSymbol = semanticModel.GetSymbolInfo(invocationSyntax).Symbol;
                if (methodSymbol is null ||
                    !requireCheckMethodSymbols.Contains(methodSymbol, SymbolEqualityComparer.Default)) {
                    continue;
                }

                requireCheckCalls.Add(
                    new RequireCheckAnalysisContext(
                        semanticModel.GetDeclaredSymbol(
                            invocationSyntax.Ancestors()
                                            .OfType<MethodDeclarationSyntax>()
                                            .First()
                        )!,
                        invocationSyntax,
                        semanticModel
                    )
                );
            }
        }

        // Every interface that has exactly one implementing class could possibly miss the "ImplementedBy" attribute
        foreach (var (interfaceSymbol, implCount) in _implementerCountByInterface) {
            if (implCount != 1) {
                continue;
            }

            foreach (var location in interfaceSymbol.Locations) {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        SealedFgaDiagnosticRules.PossiblyMisingImplementedByRule,
                        location,
                        interfaceSymbol.Name
                    )
                );
            }
        }

        // Analyze all require check calls
        foreach (var requireCheckCallContext in requireCheckCalls) {
            var cfg = ControlFlowGraph.Create(
                requireCheckCallContext.RequireCheckSyntax,
                requireCheckCallContext.SemanticModel
            );

            if (cfg == null) continue;

            // TODO: Add analysis here
        }

        // Analyze all http endpoints
        foreach (var httpEndpointMethodContext in httpEndpointMethodContexts) {
            var cfg = ControlFlowGraph.Create(
                httpEndpointMethodContext.MethodSyntax,
                httpEndpointMethodContext.MethodSemanticModel
            );

            // Should not happen?
            if (cfg == null) continue;

            // First, run PointsToAnalysis for de-duplicating reference copies
            var interproceduralAnalysisConfiguration = InterproceduralAnalysisConfiguration.Create(
                context.Options,
                SealedFgaDiagnosticRules.MissingAuthorizationRule,
                cfg,
                httpEndpointMethodContext.MethodSemanticModel.Compilation,
                InterproceduralAnalysisKind.ContextSensitive,
                defaultMaxInterproceduralMethodCallChain: 4
            );
            var pointsToAnalysisResult = PointsToAnalysis.TryGetOrComputeResult(
                cfg,
                httpEndpointMethodContext.MethodSymbol,
                context.Options,
                wellKnownTypeProvider,
                PointsToAnalysisKind.Complete,
                interproceduralAnalysisConfiguration,
                null, // TODO: Maybe override?
                pessimisticAnalysis: false, // IMPORTANT; if true, most locations are unknown
                performCopyAnalysis: false // Seems irrelevant
            )!;

            // Extract auth data from annotated parameters using lattice-based approach
            // Block at index 1 contains the parameter symbols
            var pointsToBlockAnalysisResult = pointsToAnalysisResult[cfg.Blocks[1]];
            var initialAuthorizationState = CreateInitialAuthorizationState(
                httpEndpointMethodContext,
                pointsToBlockAnalysisResult
            );

            // Execute the data flow analysis for the current HTTP endpoint method
            _ = GlobalFlowStateAnalysis.TryGetOrComputeResult(
                cfg,
                httpEndpointMethodContext.MethodSymbol,
                ctx => new SealedFgaDataFlowVisitor(
                    ctx,
                    initialAuthorizationState,
                    diagnosticsReporter
                ),
                wellKnownTypeProvider,
                context.Options,
                SealedFgaDiagnosticRules.FoundContextRule,
                true, // Needs to be true; if false, the PointsTo analysis will always be run pessimistic
                false, // Needs to be true, else we pretty much only receive unknown locations
                out _,
                InterproceduralAnalysisKind.ContextSensitive
            );
        }
    }

    /// <summary>
    ///     Creates the initial authorization state from parameter annotations.
    /// </summary>
    /// <param name="httpEndpointMethodContext">The HTTP endpoint method context</param>
    /// <param name="pointsToBlockAnalysisResult">The points-to analysis result of the parameter declaration block</param>
    /// <returns>Initial authorization state with permissions from parameter annotations</returns>
    private SealedFgaDataFlowValue CreateInitialAuthorizationState(
        HttpEndpointAnalysisContext httpEndpointMethodContext,
        PointsToBlockAnalysisResult pointsToBlockAnalysisResult
    ) {
        var authorizationBuilder = ImmutableDictionary.CreateBuilder<AbstractLocation, PermissionSet>();

        foreach (var httpParamSymbol in httpEndpointMethodContext.MethodSymbol.Parameters) {
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

                    if (relParam is not null) {
                        var httpParamPointsToVal = GetPointsToValueByParamSymbol(
                            pointsToBlockAnalysisResult,
                            httpParamSymbol
                        );
                        AddPermissionToBuilder(
                            authorizationBuilder,
                            httpParamPointsToVal.Locations,
                            relParam
                        );

                        // ID parameter permission if specified (e.g. "SecretEntityId secretId")
                        if (paramNameParam is not null) {
                            // Find the ID parameter by name
                            var idParamSymbol =
                                httpEndpointMethodContext.MethodSymbol.Parameters.FirstOrDefault(p =>
                                    p.Name == paramNameParam
                                );
                            if (idParamSymbol != null) {
                                var idParamPointsToVal = GetPointsToValueByParamSymbol(
                                    pointsToBlockAnalysisResult,
                                    idParamSymbol
                                );
                                AddPermissionToBuilder(
                                    authorizationBuilder,
                                    idParamPointsToVal.Locations,
                                    relParam
                                );
                            }
                        }
                    }
                }

                // [FgaAuthorizeList(Relation = "...")]
                if (SymbolEqualityComparer.Default.Equals(attrData.AttributeClass, fgaAuthorizeListAttributeSymbol)) {
                    if (attrData.NamedArguments.Length > 0) {
                        var relParam = (string) attrData.NamedArguments[0].Value.Value!;
                        var httpParamPointsToVal = GetPointsToValueByParamSymbol(
                            pointsToBlockAnalysisResult,
                            httpParamSymbol
                        );
                        AddPermissionToBuilder(
                            authorizationBuilder,
                            httpParamPointsToVal.Locations,
                            relParam
                        );
                    }
                }
            }
        }

        var authorizationLattice = new AuthorizationLattice(authorizationBuilder.ToImmutable());
        return new SealedFgaDataFlowValue(authorizationLattice);
    }

    /// <summary>
    ///     Retrieves the PointsToAbstractValue for a given parameter symbol from the PointsToBlockAnalysisResult.
    /// </summary>
    /// <param name="blockResult">The PointsToBlockAnalysisResult that contains data mapping entities with abstract values.</param>
    /// <param name="symbol">The parameter symbol whose PointsToAbstractValue is to be retrieved.</param>
    /// <returns>The PointsToAbstractValue associated with the provided parameter symbol.</returns>
    private static PointsToAbstractValue GetPointsToValueByParamSymbol(
        PointsToBlockAnalysisResult blockResult,
        IParameterSymbol symbol
    ) {
        var key = blockResult.Data.Keys.First(k =>
            SymbolEqualityComparer.Default.Equals(k.Symbol, symbol)
        );
        return blockResult.Data[key];
    }

    /// <summary>
    ///     Adds a permission to the authorization builder, handling existing permissions.
    /// </summary>
    /// <param name="builder">The authorization builder</param>
    /// <param name="location">The analysis entity</param>
    /// <param name="permission">The permission to add</param>
    private static void AddPermissionToBuilder(
        ImmutableDictionary<AbstractLocation, PermissionSet>.Builder builder,
        AbstractLocation location,
        string permission
    ) {
        if (builder.TryGetValue(location, out var existingPermissions)) {
            builder[location] = existingPermissions.Add(permission);
        } else {
            builder[location] = new PermissionSet([permission]);
        }
    }

    /// <summary>
    /// Adds a permission to the builder for each specified location.
    /// </summary>
    /// <param name="builder">The dictionary builder to which permissions are added.</param>
    /// <param name="locations">The collection of abstract locations associated with the permissions.</param>
    /// <param name="permission">The permission to be added for each location.</param>
    private static void AddPermissionToBuilder(
        ImmutableDictionary<AbstractLocation, PermissionSet>.Builder builder,
        IEnumerable<AbstractLocation> locations,
        string permission
    ) {
        foreach (var location in locations) {
            AddPermissionToBuilder(builder, location, permission);
        }
    }
}
