using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SealedFga.Analysis;

/// <summary>
/// Represents the authorization state in data flow analysis using lattice theory.
/// Tracks which permissions have been verified for which objects.
/// </summary>
public sealed class AuthorizationLattice : IEquatable<AuthorizationLattice>
{
    private readonly ImmutableDictionary<ObjectIdentifier, PermissionSet> _authorizations;
    private readonly bool _isTop;
    
    /// <summary>
    /// Bottom element of the lattice (no permissions verified).
    /// </summary>
    public static AuthorizationLattice Bottom { get; } = 
        new(ImmutableDictionary<ObjectIdentifier, PermissionSet>.Empty);
    
    /// <summary>
    /// Top element of the lattice (all possible permissions verified).
    /// Used for unreachable code paths or error states.
    /// </summary>
    public static AuthorizationLattice Top { get; } = 
        new(ImmutableDictionary<ObjectIdentifier, PermissionSet>.Empty, isTop: true);
    
    /// <summary>
    /// Creates a new authorization lattice with the given authorizations.
    /// </summary>
    /// <param name="authorizations">The authorization mappings</param>
    /// <param name="isTop">Whether this represents the top element</param>
    public AuthorizationLattice(ImmutableDictionary<ObjectIdentifier, PermissionSet> authorizations, bool isTop = false)
    {
        _authorizations = authorizations;
        _isTop = isTop;
    }
    
    /// <summary>
    /// Gets all object identifiers that have permissions in this lattice.
    /// </summary>
    public IEnumerable<ObjectIdentifier> TrackedObjects => _authorizations.Keys;
    
    /// <summary>
    /// Checks if this is the top element of the lattice.
    /// </summary>
    public bool IsTop => _isTop;
    
    /// <summary>
    /// Checks if this is the bottom element of the lattice.
    /// </summary>
    public bool IsBottom => !_isTop && _authorizations.IsEmpty;
    
    /// <summary>
    /// Checks if the specified object has the given permission.
    /// </summary>
    /// <param name="objectId">The object identifier</param>
    /// <param name="relation">The permission/relation to check</param>
    /// <returns>True if the permission is verified, false otherwise</returns>
    public bool HasPermission(ObjectIdentifier objectId, string relation)
    {
        if (_isTop) return true;
        
        return _authorizations.TryGetValue(objectId, out var permissions) && 
               permissions.Contains(relation);
    }
    
    /// <summary>
    /// Checks if the specified object has all of the given permissions.
    /// </summary>
    /// <param name="objectId">The object identifier</param>
    /// <param name="relations">The permissions/relations to check</param>
    /// <returns>True if all permissions are verified, false otherwise</returns>
    public bool HasAllPermissions(ObjectIdentifier objectId, IEnumerable<string> relations)
    {
        if (_isTop) return true;
        
        if (!_authorizations.TryGetValue(objectId, out var permissions))
            return false;
        
        return permissions.ContainsAll(relations);
    }
    
    /// <summary>
    /// Gets all permissions verified for the specified object.
    /// </summary>
    /// <param name="objectId">The object identifier</param>
    /// <returns>The permissions verified for the object</returns>
    public IEnumerable<string> GetPermissions(ObjectIdentifier objectId)
    {
        if (_isTop) return []; // Top element conceptually has all permissions
        
        return _authorizations.TryGetValue(objectId, out var permissions) 
            ? permissions.Permissions 
            : [];
    }
    
    /// <summary>
    /// Gets the permissions from the required set that are missing for the specified object.
    /// </summary>
    /// <param name="objectId">The object identifier</param>
    /// <param name="requiredRelations">The permissions that are required</param>
    /// <returns>The permissions that are missing</returns>
    public IEnumerable<string> GetMissingPermissions(ObjectIdentifier objectId, IEnumerable<string> requiredRelations)
    {
        if (_isTop) return []; // Top element has all permissions
        
        if (!_authorizations.TryGetValue(objectId, out var permissions))
            return requiredRelations;
        
        return permissions.GetMissingPermissions(requiredRelations);
    }
    
    /// <summary>
    /// Adds a permission for the specified object, returning a new lattice.
    /// </summary>
    /// <param name="objectId">The object identifier</param>
    /// <param name="relation">The permission/relation to add</param>
    /// <returns>A new lattice with the added permission</returns>
    public AuthorizationLattice WithPermission(ObjectIdentifier objectId, string relation)
    {
        if (_isTop) return this;
        
        var currentPermissions = _authorizations.TryGetValue(objectId, out var existing) 
            ? existing 
            : PermissionSet.Empty;
        
        var newPermissions = currentPermissions.Add(relation);
        var newAuthorizations = _authorizations.SetItem(objectId, newPermissions);
        
        return new AuthorizationLattice(newAuthorizations);
    }
    
    /// <summary>
    /// Adds multiple permissions for the specified object, returning a new lattice.
    /// </summary>
    /// <param name="objectId">The object identifier</param>
    /// <param name="relations">The permissions/relations to add</param>
    /// <returns>A new lattice with the added permissions</returns>
    public AuthorizationLattice WithPermissions(ObjectIdentifier objectId, IEnumerable<string> relations)
    {
        if (_isTop) return this;
        
        var currentPermissions = _authorizations.TryGetValue(objectId, out var existing) 
            ? existing 
            : PermissionSet.Empty;
        
        var newPermissions = currentPermissions.AddRange(relations);
        var newAuthorizations = _authorizations.SetItem(objectId, newPermissions);
        
        return new AuthorizationLattice(newAuthorizations);
    }
    
    /// <summary>
    /// Performs a join operation (union) with another lattice.
    /// Returns a lattice containing all permissions from both lattices.
    /// </summary>
    /// <param name="other">The other lattice to join with</param>
    /// <returns>A new lattice containing the union of both lattices</returns>
    public AuthorizationLattice Join(AuthorizationLattice other)
    {
        if (_isTop || other._isTop) return Top;
        if (IsBottom) return other;
        if (other.IsBottom) return this;
        
        var builder = _authorizations.ToBuilder();
        
        foreach (var (objectId, otherPermissions) in other._authorizations)
        {
            if (builder.TryGetValue(objectId, out var currentPermissions))
            {
                builder[objectId] = currentPermissions.Union(otherPermissions);
            }
            else
            {
                builder[objectId] = otherPermissions;
            }
        }
        
        return new AuthorizationLattice(builder.ToImmutable());
    }
    
    /// <summary>
    /// Performs a meet operation (intersection) with another lattice.
    /// Returns a lattice containing only permissions present in both lattices.
    /// </summary>
    /// <param name="other">The other lattice to meet with</param>
    /// <returns>A new lattice containing the intersection of both lattices</returns>
    public AuthorizationLattice Meet(AuthorizationLattice other)
    {
        if (_isTop) return other;
        if (other._isTop) return this;
        if (IsBottom || other.IsBottom) return Bottom;
        
        var builder = ImmutableDictionary.CreateBuilder<ObjectIdentifier, PermissionSet>();
        
        foreach (var (objectId, currentPermissions) in _authorizations)
        {
            if (other._authorizations.TryGetValue(objectId, out var otherPermissions))
            {
                var intersection = currentPermissions.Intersect(otherPermissions);
                if (!intersection.IsEmpty)
                {
                    builder[objectId] = intersection;
                }
            }
        }
        
        return new AuthorizationLattice(builder.ToImmutable());
    }
    
    /// <summary>
    /// Checks if this lattice is a subset of another lattice (≤ operation).
    /// </summary>
    /// <param name="other">The other lattice to compare against</param>
    /// <returns>True if this lattice is a subset of the other lattice</returns>
    public bool IsSubsetOf(AuthorizationLattice other)
    {
        if (other._isTop) return true;
        if (_isTop) return false;
        if (IsBottom) return true;
        if (other.IsBottom) return IsBottom;
        
        foreach (var (objectId, currentPermissions) in _authorizations)
        {
            if (!other._authorizations.TryGetValue(objectId, out var otherPermissions) ||
                !currentPermissions.IsSubsetOf(otherPermissions))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public bool Equals(AuthorizationLattice? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        
        if (_isTop != other._isTop) return false;
        if (_isTop && other._isTop) return true;
        
        if (_authorizations.Count != other._authorizations.Count) return false;
        
        foreach (var (objectId, permissions) in _authorizations)
        {
            if (!other._authorizations.TryGetValue(objectId, out var otherPermissions) ||
                !permissions.Equals(otherPermissions))
            {
                return false;
            }
        }
        
        return true;
    }
    
    public override bool Equals(object? obj) => obj is AuthorizationLattice other && Equals(other);
    
    public override int GetHashCode()
    {
        if (_isTop) return int.MaxValue;
        
        return _authorizations.Aggregate(0, (acc, kvp) =>
            HashCode.Combine(acc, kvp.Key.GetHashCode(), kvp.Value.GetHashCode()));
    }
    
    public override string ToString()
    {
        if (_isTop) return "⊤";
        if (IsBottom) return "⊥";
        
        var entries = _authorizations.Select(kvp => $"{kvp.Key}: {kvp.Value}");
        return $"{{ {string.Join(", ", entries)} }}";
    }
    
    public static bool operator ==(AuthorizationLattice? left, AuthorizationLattice? right) => 
        ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
    
    public static bool operator !=(AuthorizationLattice? left, AuthorizationLattice? right) => !(left == right);
}