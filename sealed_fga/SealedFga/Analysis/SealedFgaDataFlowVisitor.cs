using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;
using Microsoft.CodeAnalysis.Operations;
using SealedFga.Models;
using SealedFga.Util;

namespace SealedFga.Analysis;

internal class SealedFgaDataFlowVisitor(
    GlobalFlowStateAnalysisContext analysisContext,
    Dictionary<INamedTypeSymbol, INamedTypeSymbol> interfaceRedirects,
    CheckedPermissionsByEntityVarDict checkedPermissionsByEntityId,
    CompilationAnalysisContext diagnosticContext
) : GlobalFlowStateValueSetFlowOperationVisitor(analysisContext, true) {
    /// <summary>
    ///     Visits a method invocation.
    ///     Overriden to replace the target method if necessary to imitate dependency injection.
    ///     Identifies DB context accesses.
    /// </summary>
    public override GlobalFlowStateAnalysisValueSet VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
        IMethodSymbol method,
        IOperation? visitedInstance,
        ImmutableArray<IArgumentOperation> visitedArguments,
        bool invokedAsDelegate,
        IOperation originalOperation,
        GlobalFlowStateAnalysisValueSet defaultValue
    ) {
        if (visitedInstance != null) {
            DetectContextMethodCall(method, visitedInstance, originalOperation);
        }

        method = HandleDependencyInjection(method);
        return base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
            method,
            visitedInstance,
            visitedArguments,
            invokedAsDelegate,
            originalOperation,
            defaultValue
        );
    }

    /// <summary>
    ///     Handles dependency injection by replacing the interface with the implementing class on method invocations.
    ///     If the target method is not called on an interface or no implementing class is known, returns the input method.
    /// </summary>
    /// <param name="method">Target method.</param>
    /// <returns>Replaced method or same if no replacement is known.</returns>
    private IMethodSymbol HandleDependencyInjection(IMethodSymbol method) {
        method = method.OriginalDefinition;
        interfaceRedirects.TryGetValue(method.ContainingType, out var implementingClassSymbol);
        if (implementingClassSymbol is null) {
            return method;
        }

        return (implementingClassSymbol.FindImplementationForInterfaceMember(method) as IMethodSymbol)!;
    }

    private void DetectContextMethodCall(IMethodSymbol method, IOperation instance, IOperation originalOperation) {
        var instanceType = instance.Type;
        if (instanceType is INamedTypeSymbol namedType && InheritsFromDbContext(namedType)) {
            diagnosticContext.ReportDiagnostic(
                Diagnostic.Create(
                    SealedFgaDiagnosticRules.FoundContextRule,
                    originalOperation.Syntax.GetLocation(),
                    $"Found DbContext method call: {method.Name} on {namedType.Name}"
                )
            );
        }
    }

    private bool InheritsFromDbContext(ITypeSymbol typeSymbol) {
        var baseType = typeSymbol.BaseType;
        while (baseType != null) {
            if (baseType.Name == "DbContext" &&
                baseType.ContainingNamespace.FullName() == "Microsoft.EntityFrameworkCore") {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }
}
