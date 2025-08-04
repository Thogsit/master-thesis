using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace SealedFga.Analysis;

using SealedFgaDataFlowPermissionsDictionary = ImmutableDictionary<AbstractLocation, PermissionSet>;

/// <summary>
///     Represents the authorization state in data flow analysis using lattice theory.
///     Tracks which permissions have been verified for which objects.
/// </summary>
public sealed class AuthorizationLattice : IEquatable<AuthorizationLattice> {
    private readonly SealedFgaDataFlowPermissionsDictionary _authorizations;

    /// <summary>
    ///     Creates a new authorization lattice with the given authorizations.
    /// </summary>
    /// <param name="authorizations">The authorization mappings</param>
    /// <param name="isTop">Whether this represents the top element</param>
    public AuthorizationLattice(SealedFgaDataFlowPermissionsDictionary authorizations,
        bool isTop = false) {
        _authorizations = authorizations;
        IsTop = isTop;
    }

    /// <summary>
    ///     Bottom element of the lattice (no permissions verified).
    /// </summary>
    public static AuthorizationLattice Bottom { get; } =
        new(SealedFgaDataFlowPermissionsDictionary.Empty);

    /// <summary>
    ///     Top element of the lattice (all possible permissions verified).
    ///     Used for unreachable code paths or error states.
    /// </summary>
    public static AuthorizationLattice Top { get; } =
        new(SealedFgaDataFlowPermissionsDictionary.Empty, true);

    /// <summary>
    ///     Gets all analysis entities that have at least one checked permission in this lattice.
    /// </summary>
    public IEnumerable<AbstractLocation> TrackedLocations => _authorizations.Keys;

    /// <summary>
    ///     Checks if this is the top element of the lattice.
    /// </summary>
    public bool IsTop { get; }

    /// <summary>
    ///     Checks if this is the bottom element of the lattice.
    /// </summary>
    public bool IsBottom => !IsTop && _authorizations.IsEmpty;

    public bool Equals(AuthorizationLattice? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (IsTop != other.IsTop) return false;
        if (IsTop && other.IsTop) return true;

        if (_authorizations.Count != other._authorizations.Count) return false;

        foreach (var (entity, permissions) in _authorizations) {
            if (!other._authorizations.TryGetValue(entity, out var otherPermissions) ||
                !permissions.Equals(otherPermissions)) {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Checks if the specified entity has the given permission.
    /// </summary>
    /// <param name="location">The analysis entity</param>
    /// <param name="relation">The permission/relation to check</param>
    /// <returns>True if the permission is verified, false otherwise</returns>
    public bool HasPermission(AbstractLocation location, string relation) {
        if (IsTop) return true;

        return _authorizations.TryGetValue(location, out var permissions)
               && permissions.Contains(relation);
    }

    /// <summary>
    ///     Checks if the specified entity has all of the given permissions.
    /// </summary>
    /// <param name="location">The analysis entity</param>
    /// <param name="relations">The permissions/relations to check</param>
    /// <returns>True if all permissions are verified, false otherwise</returns>
    public bool HasAllPermissions(AbstractLocation location, IReadOnlyCollection<string> relations) {
        if (IsTop || relations.Count == 0) return true;

        return _authorizations.TryGetValue(location, out var permissions)
               && permissions.ContainsAll(relations);
    }

    /// <summary>
    ///     Gets the permissions from the required set that are missing for the specified entity.
    /// </summary>
    /// <param name="location">The analysis entity</param>
    /// <param name="requiredRelations">The permissions that are required</param>
    /// <returns>The permissions that are missing</returns>
    public IEnumerable<string> GetMissingPermissions(AbstractLocation location, IEnumerable<string> requiredRelations) {
        // Top element has all permissions, none could be missing
        if (IsTop) return [];

        if (!_authorizations.TryGetValue(location, out var permissions)) {
            return requiredRelations;
        }

        return permissions.GetMissingPermissions(requiredRelations);
    }

    /// <summary>
    ///     Adds a permission for the specified entity, returning a new lattice.
    /// </summary>
    /// <param name="location">The analysis entity</param>
    /// <param name="relation">The permission/relation to add</param>
    /// <returns>A new lattice with the added permission</returns>
    public AuthorizationLattice WithPermission(AbstractLocation location, string relation) {
        // Already has all permissions
        if (IsTop) return this;

        var currentPermissions = _authorizations.TryGetValue(location, out var existing)
            ? existing
            : PermissionSet.Empty;

        var newPermissions = currentPermissions.Add(relation);
        var newAuthorizations = _authorizations.SetItem(location, newPermissions);

        return new AuthorizationLattice(newAuthorizations);
    }

    /// <summary>
    ///     Adds multiple permissions for the specified entity, returning a new lattice.
    /// </summary>
    /// <param name="location">The analysis entity</param>
    /// <param name="relations">The permissions/relations to add</param>
    /// <returns>A new lattice with the added permissions</returns>
    public AuthorizationLattice WithPermissions(AbstractLocation location, IEnumerable<string> relations) {
        // Already has all permissions
        if (IsTop) return this;

        var currentPermissions = _authorizations.TryGetValue(location, out var existing)
            ? existing
            : PermissionSet.Empty;

        var newPermissions = currentPermissions.AddRange(relations);
        var newAuthorizations = _authorizations.SetItem(location, newPermissions);

        return new AuthorizationLattice(newAuthorizations);
    }

    /// <summary>
    ///     Performs a meet operation (intersection) with another lattice.
    ///     Returns a lattice containing only permissions present in both lattices.
    /// </summary>
    /// <param name="other">The other lattice to meet with</param>
    /// <returns>A new lattice containing the intersection of both lattices</returns>
    public AuthorizationLattice Meet(AuthorizationLattice other) {
        /* MEET with top or bottom element
         * +-- A ---+-- B ---+--- R ----+
         * |   ⊤    |   *    |    B     |
         * |   A    |   ⊤    |    A     |
         * |   ⊥    |   *    |    ⊥     |
         * |   *    |   ⊥    |    ⊥     |
         * +--------+--------+----------+
         */
        if (IsTop) return other;
        if (other.IsTop) return this;
        if (IsBottom || other.IsBottom) return Bottom;

        /* MEET with another lattice
         * +-- A ---+-- B ---+--- R ----+
         * |   A    |   B    |  A ∩ B   |
         * +--------+--------+----------+
         */
        var builder = ImmutableDictionary.CreateBuilder<AbstractLocation, PermissionSet>();
        foreach (var (entity, currentPermissions) in _authorizations) {
            if (other._authorizations.TryGetValue(entity, out var otherPermissions)) {
                var intersection = currentPermissions.Intersect(otherPermissions);
                if (!intersection.IsEmpty) {
                    builder[entity] = intersection;
                }
            }
        }

        return new AuthorizationLattice(builder.ToImmutable());
    }

    /// <summary>
    ///     Performs a join operation (union) with another lattice.
    ///     Returns a lattice containing all permissions from both lattices.
    /// </summary>
    /// <param name="other">The other lattice to join with</param>
    /// <returns>A new lattice containing the union of both lattices</returns>
    public AuthorizationLattice Join(AuthorizationLattice other) {
        /* JOIN with top or bottom element
         * +-- A ---+-- B ---+--- R ----+
         * |   ⊤    |   *    |    ⊤     |
         * |   *    |   ⊤    |    ⊤     |
         * |   ⊥    |   *    |    B     |
         * |   A    |   ⊥    |    A     |
         * +--------+--------+----------+
         */
        if (IsTop || other.IsTop) return Top;
        if (IsBottom) return other;
        if (other.IsBottom) return this;

        /* JOIN with another lattice
         * +-- A ---+-- B ---+--- R ----+
         * |   A    |   B    |  A ∪ B   |
         * +--------+--------+----------+
         */
        var builder = _authorizations.ToBuilder();
        foreach (var (entity, otherPermissions) in other._authorizations) {
            if (builder.TryGetValue(entity, out var currentPermissions)) {
                builder[entity] = currentPermissions.Union(otherPermissions);
            } else {
                builder[entity] = otherPermissions;
            }
        }

        return new AuthorizationLattice(builder.ToImmutable());
    }

    /// <summary>
    ///     Checks if this lattice is a subset of another lattice (≤ operation).
    /// </summary>
    /// <param name="other">The other lattice to compare against</param>
    /// <returns>True if this lattice is a subset of the other lattice</returns>
    public bool IsSubsetOf(AuthorizationLattice other) {
        /* SUBSET with top or bottom element
         * +-- A ---+-- B ---+--- R ----+
         * |   ⊤    |   *    |  false   |
         * |   *    |   ⊤    |   true   |
         * |   ⊥    |   *    |   true   |
         * |   A    |   ⊥    |  false   |
         * +--------+--------+----------+
         */
        if (IsTop) return false;
        if (other.IsTop) return true;
        if (IsBottom) return true;
        if (other.IsBottom) return IsBottom;

        /* SUBSET with another lattice
         * +-- A ---+-- B ---+--- R ----+
         * |   A    |   B    |  A ⊆ B   |
         * +--------+--------+----------+
         */
        foreach (var (entity, currentPermissions) in _authorizations) {
            if (!other._authorizations.TryGetValue(entity, out var otherPermissions)
                || !currentPermissions.IsSubsetOf(otherPermissions)) {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Calculates the intersection of permissions across a collection of locations.
    /// </summary>
    /// <param name="locations">The collection of locations for which to calculate the intersected permissions.</param>
    /// <returns>The intersected <see cref="PermissionSet"/> of all provided locations, or an empty set if no permissions are available.</returns>
    public PermissionSet GetPermissionIntersect(IEnumerable<AbstractLocation> locations) {
        PermissionSet? permissionsIntersect = null;
        foreach (var location in locations) {
            permissionsIntersect ??= _authorizations[location]; // For first location
            permissionsIntersect.Union(_authorizations[location]);
        }

        return permissionsIntersect ?? PermissionSet.Empty; // Fallback to no checked permissions
    }

    /// <summary>
    ///     Retrieves the permissions associated with the specified location
    ///     or returns an empty permissions set if none are found.
    /// </summary>
    /// <param name="location">The location for which to retrieve permissions.</param>
    /// <returns>
    ///     A <see cref="PermissionSet"/> containing the permissions associated with the given location,
    ///     or an empty set if no permissions are found.
    /// </returns>
    public PermissionSet GetPermissionsOrEmpty(AbstractLocation location)
        => _authorizations.TryGetValue(location, out var permissions) ? permissions : PermissionSet.Empty;

    public override bool Equals(object? obj) => obj is AuthorizationLattice other && Equals(other);

    public override int GetHashCode() {
        if (IsTop) return int.MaxValue;

        return _authorizations.Aggregate(
            0,
            (acc, kvp) =>
                HashCode.Combine(
                    acc,
                    kvp.Key.GetHashCode(),
                    kvp.Value.GetHashCode()
                )
        );
    }

    public override string ToString() {
        if (IsTop) return "⊤";
        if (IsBottom) return "⊥";

        var entries = _authorizations.Select(kvp => $"{kvp.Key}: {kvp.Value}");
        return $"{{ {string.Join(", ", entries)} }}";
    }

    public static bool operator ==(AuthorizationLattice? left, AuthorizationLattice? right) =>
        ReferenceEquals(left, right) || (left?.Equals(right) ?? false);

    public static bool operator !=(AuthorizationLattice? left, AuthorizationLattice? right) => !(left == right);
}
