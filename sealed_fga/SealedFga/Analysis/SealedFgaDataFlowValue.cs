using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;

namespace SealedFga.Analysis;

/// <summary>
/// Represents the authorization state during data flow analysis.
/// Contains information about which permissions have been verified for which objects.
/// </summary>
internal sealed class SealedFgaDataFlowValue : IAbstractAnalysisValue, IEquatable<SealedFgaDataFlowValue>
{
    /// <summary>
    /// The authorization lattice containing all verified permissions.
    /// </summary>
    public AuthorizationLattice AuthorizationState { get; }

    /// <summary>
    /// Indicates whether this value represents a negated state (used for control flow).
    /// </summary>
    public bool Negated { get; }

    /// <summary>
    /// Creates a new SealedFgaDataFlowValue with the specified authorization state.
    /// </summary>
    /// <param name="authorizationState">The authorization lattice</param>
    /// <param name="negated">Whether this represents a negated state</param>
    public SealedFgaDataFlowValue(AuthorizationLattice authorizationState, bool negated = false)
    {
        AuthorizationState = authorizationState;
        Negated = negated;
    }

    /// <summary>
    /// Creates a new SealedFgaDataFlowValue with the bottom authorization state.
    /// </summary>
    public SealedFgaDataFlowValue() : this(AuthorizationLattice.Bottom, false)
    {
    }

    /// <summary>
    /// Legacy constructor for backward compatibility during migration.
    /// </summary>
    /// <param name="checkedPermissionsByEntityId">Legacy permission dictionary</param>
    /// <param name="negated">Whether this represents a negated state</param>
    [Obsolete("Use constructor with AuthorizationLattice instead")]
    public SealedFgaDataFlowValue(Dictionary<string, HashSet<string>> checkedPermissionsByEntityId, bool negated = false)
    {
        // Convert legacy format to new lattice structure
        var builder = System.Collections.Immutable.ImmutableDictionary.CreateBuilder<ObjectIdentifier, PermissionSet>();

        foreach (var (entityId, permissions) in checkedPermissionsByEntityId)
        {
            // Parse legacy format "type:id" or use as variable name
            var objectId = ParseLegacyEntityId(entityId);
            builder[objectId] = new PermissionSet(permissions);
        }

        AuthorizationState = new AuthorizationLattice(builder.ToImmutable());
        Negated = negated;
    }

    /// <summary>
    /// Parses a legacy entity ID string into an ObjectIdentifier.
    /// </summary>
    /// <param name="entityId">The legacy entity ID string</param>
    /// <returns>An ObjectIdentifier representing the entity</returns>
    private static ObjectIdentifier ParseLegacyEntityId(string entityId)
    {
        // Check if it's in the format "type:id"
        var colonIndex = entityId.IndexOf(':');
        if (colonIndex > 0 && colonIndex < entityId.Length - 1)
        {
            var entityType = entityId.Substring(0, colonIndex);
            var idValue = entityId.Substring(colonIndex + 1);
            return new ObjectIdentifier.EntityId(entityType, idValue);
        }

        // For non-colon format, treat as entity ID with unknown type
        return new ObjectIdentifier.EntityId("unknown", entityId);
    }

    /// <summary>
    /// Adds a permission for the specified object, returning a new data flow value.
    /// </summary>
    /// <param name="objectId">The object identifier</param>
    /// <param name="relation">The permission/relation to add</param>
    /// <returns>A new data flow value with the added permission</returns>
    public SealedFgaDataFlowValue WithPermission(ObjectIdentifier objectId, string relation)
    {
        var newAuthorizationState = AuthorizationState.WithPermission(objectId, relation);
        return new SealedFgaDataFlowValue(newAuthorizationState, Negated);
    }

    /// <summary>
    /// Adds multiple permissions for the specified object, returning a new data flow value.
    /// </summary>
    /// <param name="objectId">The object identifier</param>
    /// <param name="relations">The permissions/relations to add</param>
    /// <returns>A new data flow value with the added permissions</returns>
    public SealedFgaDataFlowValue WithPermissions(ObjectIdentifier objectId, IEnumerable<string> relations)
    {
        var newAuthorizationState = AuthorizationState.WithPermissions(objectId, relations);
        return new SealedFgaDataFlowValue(newAuthorizationState, Negated);
    }

    /// <summary>
    /// Performs a join operation with another data flow value (lattice union).
    /// </summary>
    /// <param name="other">The other data flow value</param>
    /// <returns>A new data flow value containing the union of both authorization states</returns>
    public SealedFgaDataFlowValue Join(SealedFgaDataFlowValue other)
    {
        var newAuthorizationState = AuthorizationState.Join(other.AuthorizationState);
        return new SealedFgaDataFlowValue(newAuthorizationState, Negated || other.Negated);
    }

    /// <summary>
    /// Performs a meet operation with another data flow value (lattice intersection).
    /// </summary>
    /// <param name="other">The other data flow value</param>
    /// <returns>A new data flow value containing the intersection of both authorization states</returns>
    public SealedFgaDataFlowValue Meet(SealedFgaDataFlowValue other)
    {
        var newAuthorizationState = AuthorizationState.Meet(other.AuthorizationState);
        return new SealedFgaDataFlowValue(newAuthorizationState, Negated && other.Negated);
    }

    /// <summary>
    /// Checks if the specified object has the given permission.
    /// </summary>
    /// <param name="objectId">The object identifier</param>
    /// <param name="relation">The permission/relation to check</param>
    /// <returns>True if the permission is verified, false otherwise</returns>
    public bool HasPermission(ObjectIdentifier objectId, string relation)
        => AuthorizationState.HasPermission(objectId, relation);

    /// <summary>
    /// Checks if the specified object has all of the given permissions.
    /// </summary>
    /// <param name="objectId">The object identifier</param>
    /// <param name="relations">The permissions/relations to check</param>
    /// <returns>True if all permissions are verified, false otherwise</returns>
    public bool HasAllPermissions(ObjectIdentifier objectId, IEnumerable<string> relations)
        => AuthorizationState.HasAllPermissions(objectId, relations);

    /// <summary>
    /// Gets the permissions that are missing for the specified object.
    /// </summary>
    /// <param name="objectId">The object identifier</param>
    /// <param name="requiredRelations">The permissions that are required</param>
    /// <returns>The permissions that are missing</returns>
    public IEnumerable<string> GetMissingPermissions(ObjectIdentifier objectId, IEnumerable<string> requiredRelations)
        => AuthorizationState.GetMissingPermissions(objectId, requiredRelations);

    public IAbstractAnalysisValue GetNegatedValue()
        => new SealedFgaDataFlowValue(AuthorizationState, !Negated);

    public bool Equals(IAbstractAnalysisValue other)
        => other is SealedFgaDataFlowValue otherValue && Equals(otherValue);

    public bool Equals(SealedFgaDataFlowValue? other)
    {
        return other is not null &&
               AuthorizationState.Equals(other.AuthorizationState) &&
               Negated == other.Negated;
    }

    public override bool Equals(object? obj) => obj is SealedFgaDataFlowValue other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(AuthorizationState, Negated);

    public override string ToString() => $"{{ AuthorizationState: {AuthorizationState}, Negated: {Negated} }}";
}
