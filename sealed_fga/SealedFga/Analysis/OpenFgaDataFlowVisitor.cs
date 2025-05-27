using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;
using Microsoft.CodeAnalysis.Operations;
using SealedFga.Models;

namespace SealedFga.Analysis;

internal class OpenFgaDataFlowVisitor(
    GlobalFlowStateAnalysisContext analysisContext,
    Dictionary<INamedTypeSymbol, INamedTypeSymbol> interfaceRedirects,
    CheckedPermissionsByEntityVarDict checkedPermissionsByEntityId
) : GlobalFlowStateValueSetFlowOperationVisitor(analysisContext, hasPredicatedGlobalState: true)
{
    
    /// <summary>
    ///     Visits a method invocation.
    ///     Overriden to replace the target method if necessary to imitate dependency injection.
    /// </summary>
    public override GlobalFlowStateAnalysisValueSet VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
        IMethodSymbol method,
        IOperation? visitedInstance,
        ImmutableArray<IArgumentOperation> visitedArguments,
        bool invokedAsDelegate,
        IOperation originalOperation,
        GlobalFlowStateAnalysisValueSet defaultValue)
    {
        method = HandleDependencyInjection(method);
        var value = base.VisitInvocation_NonLambdaOrDelegateOrLocalFunction(
            method,
            visitedInstance,
            visitedArguments,
            invokedAsDelegate,
            originalOperation,
            defaultValue
        );

        return value;
    }

    /// <summary>
    ///     Handles dependency injection by replacing the interface with the implementing class on method invocations.
    ///     If the target method is not called on an interface or no implementing class is known, returns the input method.
    /// </summary>
    /// <param name="method">Target method.</param>
    /// <returns>Replaced method or same if no replacement is known.</returns>
    private IMethodSymbol HandleDependencyInjection(IMethodSymbol method)
    {
        method = method.OriginalDefinition;
        interfaceRedirects.TryGetValue(method.ContainingType, out var implementingClassSymbol);
        if (implementingClassSymbol is null)
        {
            return method;
        }

        return (implementingClassSymbol.FindImplementationForInterfaceMember(method) as IMethodSymbol)!;
    }
}