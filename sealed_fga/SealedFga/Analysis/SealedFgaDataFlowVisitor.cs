using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;
using Microsoft.CodeAnalysis.Operations;
using SealedFga.Fga;
using SealedFga.Util;

namespace SealedFga.Analysis;

internal class SealedFgaDataFlowVisitor(
    GlobalFlowStateAnalysisContext analysisContext,
    Dictionary<INamedTypeSymbol, INamedTypeSymbol> interfaceRedirects,
    SealedFgaDataFlowValue initialAuthorizationState,
    CompilationAnalysisContext diagnosticContext
) : GlobalFlowStateValueSetFlowOperationVisitor(analysisContext, true) {
    /// <summary>
    ///     Visits a method invocation.
    ///     Overriden to replace the target method if necessary to imitate dependency injection.
    ///     Identifies DB context accesses and handles authorization checks.
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

        // Handle authorization method calls
        var updatedValue = HandleAuthorizationMethodCall(method, visitedArguments, originalOperation, defaultValue);
        if (updatedValue != null) {
            return updatedValue;
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
    /// Handles authorization method calls (CheckAsync, EnsureCheckAsync, RequireCheck).
    /// Returns updated value set for authorization calls, or defaultValue for others.
    /// </summary>
    /// <param name="method">The method being called</param>
    /// <param name="visitedArguments">The arguments to the method call</param>
    /// <param name="originalOperation">The original operation</param>
    /// <param name="defaultValue">The default value set (current authorization state)</param>
    /// <returns>Processed value set or null if no authorization method was called</returns>
    private GlobalFlowStateAnalysisValueSet? HandleAuthorizationMethodCall(
        IMethodSymbol method,
        ImmutableArray<IArgumentOperation> visitedArguments,
        IOperation originalOperation,
        GlobalFlowStateAnalysisValueSet defaultValue) {
        var containingType = method.ContainingType;
        var methodName = method.Name;

        // Check for SealedFgaService.CheckAsync or SealedFgaService.EnsureCheckAsync
        if (containingType.Name == nameof(SealedFgaService) &&
            containingType.ContainingNamespace.FullName() == Settings.FgaNamespace &&
            methodName is nameof(SealedFgaService.CheckAsync) or nameof(SealedFgaService.EnsureCheckAsync)) {
            return HandleSealedFgaServiceCall(method, visitedArguments, originalOperation, defaultValue);
        }

        // Check for SealedFga.RequireCheck
        if (containingType.Name == nameof(SealedFgaGuard) &&
            containingType.ContainingNamespace.FullName() == Settings.PackageNamespace &&
            methodName == nameof(SealedFgaGuard.RequireCheck)) {
            return HandleRequireCheckCall(method, visitedArguments, originalOperation, defaultValue);
        }

        // For non-authorization methods, return null to indicate no change
        return null;
    }

    /// <summary>
    /// Handles SealedFgaService.CheckAsync and SealedFgaService.EnsureCheckAsync calls.
    /// These calls add permissions to the authorization state.
    /// </summary>
    private GlobalFlowStateAnalysisValueSet HandleSealedFgaServiceCall(
        IMethodSymbol method,
        ImmutableArray<IArgumentOperation> visitedArguments,
        IOperation originalOperation,
        GlobalFlowStateAnalysisValueSet defaultValue) {
        if (visitedArguments.Length < 3) {
            return defaultValue;
        }

        try {
            // Extract object identifier from arguments
            // Arguments: user, relation, objectId, (optional cancellationToken)
            var objectIdArgument = visitedArguments[2]; // Third argument is objectId
            var relationArgument = visitedArguments[1]; // Second argument is relation

            var objectId = ExtractObjectIdentifier(objectIdArgument);
            var relation = ExtractRelationName(relationArgument);

            if (objectId != null && relation != null) {
                // Create new authorization state with the added permission
                return CreateValueWithPermission(defaultValue, objectId, relation);
            }
        } catch {
            // If extraction fails, return default value
        }

        return defaultValue;
    }

    /// <summary>
    /// Handles SealedFga.RequireCheck calls.
    /// These calls verify that required permissions exist in the current state.
    /// </summary>
    private GlobalFlowStateAnalysisValueSet HandleRequireCheckCall(
        IMethodSymbol method,
        ImmutableArray<IArgumentOperation> visitedArguments,
        IOperation originalOperation,
        GlobalFlowStateAnalysisValueSet defaultValue) {
        if (visitedArguments.Length < 2) {
            return defaultValue;
        }

        try {
            // Extract object identifier and required relations
            // Arguments: entity/entityId, relations...
            var entityArgument = visitedArguments[0];
            var relationsArgument = visitedArguments[1];

            var objectId = ExtractObjectIdentifier(entityArgument);
            var requiredRelations = ExtractRequiredRelations(relationsArgument).ToList();

            if (objectId != null && requiredRelations.Any()) {
                // Check if all required permissions exist in current state
                ValidateRequiredPermissions(objectId, requiredRelations, originalOperation, defaultValue);
            }
        } catch {
            // If extraction fails, ignore silently
        }

        // RequireCheck doesn't modify the authorization state, just validates it
        return defaultValue;
    }

    /// <summary>
    /// Extracts an ObjectIdentifier from an IOperation, handling both direct operations and argument operations.
    /// </summary>
    private ObjectIdentifier? ExtractObjectIdentifier(IOperation? operation) {
        if (operation == null) {
            return null;
        }

        // Handle argument operations by extracting the underlying value
        if (operation is IArgumentOperation argument) {
            return ExtractObjectIdentifier(argument.Value);
        }

        // Handle conversion operations by unwrapping to the underlying operand
        if (operation is IConversionOperation conversion) {
            return ExtractObjectIdentifier(conversion.Operand);
        }

        // Handle parameter references
        if (operation is IParameterReferenceOperation paramRef) {
            return new ObjectIdentifier.ParameterReference(paramRef.Parameter.Type);
        }

        // Handle local references
        if (operation is ILocalReferenceOperation localRef) {
            return new ObjectIdentifier.VariableReference(localRef.Local.Type);
        }

        // Handle property access
        if (operation is IPropertyReferenceOperation propRef) {
            var baseObject = ExtractObjectIdentifier(propRef.Instance);
            if (baseObject != null) {
                return new ObjectIdentifier.PropertyAccess(baseObject, propRef.Property.Name);
            }
        }

        // TODO: Handle other operation types as needed

        return null;
    }

    /// <summary>
    /// Extracts a relation name from an argument operation.
    /// </summary>
    private string? ExtractRelationName(IArgumentOperation argument) {
        var valueOperation = argument.Value;

        // Handle property access (e.g., SecretEntityIdAttributes.can_view)
        if (valueOperation is IPropertyReferenceOperation propRef) {
            return propRef.Property.Name;
        }

        // Handle field access
        if (valueOperation is IFieldReferenceOperation fieldRef) {
            return fieldRef.Field.Name;
        }

        // Handle literal values
        if (valueOperation is ILiteralOperation literal && literal.ConstantValue.HasValue) {
            return literal.ConstantValue.Value?.ToString();
        }

        return null;
    }

    /// <summary>
    /// Extracts required relations from a params array argument.
    /// </summary>
    private IEnumerable<string> ExtractRequiredRelations(IArgumentOperation argument) {
        var valueOperation = argument.Value;

        // Handle array creation
        if (valueOperation is IArrayCreationOperation arrayCreation) {
            if (arrayCreation.Initializer != null) {
                foreach (var element in arrayCreation.Initializer.ElementValues) {
                    if (element is IPropertyReferenceOperation propRef) {
                        yield return propRef.Property.Name;
                    } else if (element is IFieldReferenceOperation fieldRef) {
                        yield return fieldRef.Field.Name;
                    }
                }
            }
        }

        // Handle single element (converted to array)
        if (valueOperation is IConversionOperation conversion) {
            if (conversion.Operand is IPropertyReferenceOperation propRef) {
                yield return propRef.Property.Name;
            } else if (conversion.Operand is IFieldReferenceOperation fieldRef) {
                yield return fieldRef.Field.Name;
            }
        }
    }

    /// <summary>
    /// Creates a new value set with an added permission.
    /// </summary>
    private GlobalFlowStateAnalysisValueSet CreateValueWithPermission(
        GlobalFlowStateAnalysisValueSet currentValue,
        ObjectIdentifier objectId,
        string relation) {
        // Get the current SealedFgaDataFlowValue or create a new one
        var currentDataFlowValue = GetOrCreateDataFlowValue(currentValue);

        // Add the permission to the authorization state
        var newDataFlowValue = currentDataFlowValue.WithPermission(objectId, relation);

        // Return new value set with updated authorization state
        return ValueSetFactory.Create(newDataFlowValue);
    }

    /// <summary>
    /// Validates that required permissions exist in the current state.
    /// </summary>
    private void ValidateRequiredPermissions(
        ObjectIdentifier objectId,
        IEnumerable<string> requiredRelations,
        IOperation originalOperation,
        GlobalFlowStateAnalysisValueSet currentValue) {
        var currentDataFlowValue = GetOrCreateDataFlowValue(currentValue);
        var missingPermissions = currentDataFlowValue.GetMissingPermissions(objectId, requiredRelations).ToList();

        if (missingPermissions.Any()) {
            // Report diagnostic for missing authorization
            diagnosticContext.ReportDiagnostic(
                Diagnostic.Create(
                    SealedFgaDiagnosticRules.MissingAuthorizationRule,
                    originalOperation.Syntax.GetLocation(),
                    objectId.ToString(),
                    string.Join(", ", missingPermissions)
                )
            );
        }
    }

    /// <summary>
    /// Gets the current SealedFgaDataFlowValue from the value set or uses the initial authorization state.
    /// </summary>
    private SealedFgaDataFlowValue GetOrCreateDataFlowValue(GlobalFlowStateAnalysisValueSet valueSet) {
        // Check if the value set contains our authorization data
        if (valueSet.Kind == GlobalFlowStateAnalysisValueSetKind.Known &&
            valueSet.AnalysisValues.Count > 0) {
            // Look for a SealedFgaDataFlowValue in the analysis values
            foreach (var analysisValue in valueSet.AnalysisValues) {
                if (analysisValue is SealedFgaDataFlowValue dataFlowValue) {
                    return dataFlowValue;
                }
            }
        }

        // If no authorization data found, use the initial authorization state
        return initialAuthorizationState;
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
