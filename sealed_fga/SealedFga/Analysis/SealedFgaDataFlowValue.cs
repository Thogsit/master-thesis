using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;

namespace SealedFga.Analysis;

/// <summary>
///     Represents the authorization state during data flow analysis.
///     Contains information about which permissions have been verified for which objects.
/// </summary>
internal sealed class SealedFgaDataFlowValue : IAbstractAnalysisValue, IEquatable<SealedFgaDataFlowValue> {
    /// <summary>
    ///     Creates a new SealedFgaDataFlowValue with the specified authorization state.
    /// </summary>
    /// <param name="authorizationState">The authorization lattice</param>
    /// <param name="negated">Whether this represents a negated state</param>
    public SealedFgaDataFlowValue(AuthorizationLattice authorizationState, bool negated = false) {
        AuthorizationState = authorizationState;
        Negated = negated;
    }

    /// <summary>
    ///     Creates a new SealedFgaDataFlowValue with the bottom authorization state.
    /// </summary>
    public SealedFgaDataFlowValue() : this(AuthorizationLattice.Bottom) {
    }

    /// <summary>
    ///     The authorization lattice containing all verified permissions.
    /// </summary>
    public AuthorizationLattice AuthorizationState { get; }

    /// <summary>
    ///     Indicates whether this value represents a negated state (used for control flow).
    /// </summary>
    public bool Negated { get; }

    public IAbstractAnalysisValue GetNegatedValue()
        => new SealedFgaDataFlowValue(AuthorizationState, !Negated);

    public bool Equals(IAbstractAnalysisValue other)
        => other is SealedFgaDataFlowValue otherValue && Equals(otherValue);

    public override string ToString() => $"{{ AuthorizationState: {AuthorizationState}, Negated: {Negated} }}";

    public bool Equals(SealedFgaDataFlowValue? other)
        => other is not null &&
           AuthorizationState.Equals(other.AuthorizationState) &&
           Negated == other.Negated;

    /// <summary>
    ///     Adds a permission for the specified entity, returning a new data flow value.
    /// </summary>
    /// <param name="entity">The analysis entity</param>
    /// <param name="relation">The permission/relation to add</param>
    /// <returns>A new data flow value with the added permission</returns>
    public SealedFgaDataFlowValue WithPermission(AnalysisEntity entity, string relation) {
        var newAuthorizationState = AuthorizationState.WithPermission(entity, relation);
        return new SealedFgaDataFlowValue(newAuthorizationState, Negated);
    }

    /// <summary>
    ///     Adds multiple permissions for the specified entity, returning a new data flow value.
    /// </summary>
    /// <param name="entity">The analysis entity</param>
    /// <param name="relations">The permissions/relations to add</param>
    /// <returns>A new data flow value with the added permissions</returns>
    public SealedFgaDataFlowValue WithPermissions(AnalysisEntity entity, IEnumerable<string> relations) {
        var newAuthorizationState = AuthorizationState.WithPermissions(entity, relations);
        return new SealedFgaDataFlowValue(newAuthorizationState, Negated);
    }

    /// <summary>
    ///     Performs a join operation with another data flow value (lattice union).
    /// </summary>
    /// <param name="other">The other data flow value</param>
    /// <returns>A new data flow value containing the union of both authorization states</returns>
    public SealedFgaDataFlowValue Join(SealedFgaDataFlowValue other) {
        var newAuthorizationState = AuthorizationState.Join(other.AuthorizationState);
        return new SealedFgaDataFlowValue(newAuthorizationState, Negated || other.Negated);
    }

    /// <summary>
    ///     Performs a meet operation with another data flow value (lattice intersection).
    /// </summary>
    /// <param name="other">The other data flow value</param>
    /// <returns>A new data flow value containing the intersection of both authorization states</returns>
    public SealedFgaDataFlowValue Meet(SealedFgaDataFlowValue other) {
        var newAuthorizationState = AuthorizationState.Meet(other.AuthorizationState);
        return new SealedFgaDataFlowValue(newAuthorizationState, Negated && other.Negated);
    }

    /// <summary>
    ///     Checks if the specified entity has the given permission.
    /// </summary>
    /// <param name="entity">The analysis entity</param>
    /// <param name="relation">The permission/relation to check</param>
    /// <returns>True if the permission is verified, false otherwise</returns>
    public bool HasPermission(AnalysisEntity entity, string relation)
        => AuthorizationState.HasPermission(entity, relation);

    /// <summary>
    ///     Checks if the specified entity has all of the given permissions.
    /// </summary>
    /// <param name="entity">The analysis entity</param>
    /// <param name="relations">The permissions/relations to check</param>
    /// <returns>True if all permissions are verified, false otherwise</returns>
    public bool HasAllPermissions(AnalysisEntity entity, IReadOnlyCollection<string> relations)
        => AuthorizationState.HasAllPermissions(entity, relations);

    /// <summary>
    ///     Gets the permissions that are missing for the specified entity.
    /// </summary>
    /// <param name="entity">The analysis entity</param>
    /// <param name="requiredRelations">The permissions that are required</param>
    /// <returns>The permissions that are missing</returns>
    public IEnumerable<string> GetMissingPermissions(AnalysisEntity entity, IEnumerable<string> requiredRelations)
        => AuthorizationState.GetMissingPermissions(entity, requiredRelations);

    public override bool Equals(object? obj) => obj is SealedFgaDataFlowValue other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(AuthorizationState, Negated);
}
