using System;
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
    /// <param name="location">The analysis entity</param>
    /// <param name="relation">The permission/relation to add</param>
    /// <returns>A new data flow value with the added permission</returns>
    public SealedFgaDataFlowValue WithPermission(AbstractLocation location, string relation) {
        var newAuthorizationState = AuthorizationState.WithPermission(location, relation);
        return new SealedFgaDataFlowValue(newAuthorizationState, Negated);
    }

    public override bool Equals(object? obj) => obj is SealedFgaDataFlowValue other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(AuthorizationState, Negated);
}
