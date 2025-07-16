using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SealedFga.Analysis;

/// <summary>
/// Represents a set of permissions with lattice operations for data flow analysis.
/// Provides union, intersection, and subset operations for permission tracking.
/// </summary>
public sealed class PermissionSet : IEquatable<PermissionSet>
{
    private readonly ImmutableHashSet<string> _permissions;
    
    /// <summary>
    /// Empty permission set (bottom element).
    /// </summary>
    public static PermissionSet Empty { get; } = new(ImmutableHashSet<string>.Empty);
    
    /// <summary>
    /// Creates a new permission set with the given permissions.
    /// </summary>
    /// <param name="permissions">The permissions to include in the set</param>
    public PermissionSet(ImmutableHashSet<string> permissions)
    {
        _permissions = permissions;
    }
    
    /// <summary>
    /// Creates a new permission set with the given permissions.
    /// </summary>
    /// <param name="permissions">The permissions to include in the set</param>
    public PermissionSet(IEnumerable<string> permissions)
    {
        _permissions = permissions.ToImmutableHashSet();
    }
    
    /// <summary>
    /// Creates a new permission set with a single permission.
    /// </summary>
    /// <param name="permission">The permission to include in the set</param>
    public PermissionSet(string permission)
    {
        _permissions = ImmutableHashSet.Create(permission);
    }
    
    /// <summary>
    /// Gets all permissions in this set.
    /// </summary>
    public IEnumerable<string> Permissions => _permissions;
    
    /// <summary>
    /// Checks if this permission set is empty.
    /// </summary>
    public bool IsEmpty => _permissions.IsEmpty;
    
    /// <summary>
    /// Checks if this permission set contains the specified permission.
    /// </summary>
    /// <param name="permission">The permission to check for</param>
    /// <returns>True if the permission is in the set, false otherwise</returns>
    public bool Contains(string permission) => _permissions.Contains(permission);
    
    /// <summary>
    /// Checks if this permission set contains all of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions to check for</param>
    /// <returns>True if all permissions are in the set, false otherwise</returns>
    public bool ContainsAll(IEnumerable<string> permissions) => permissions.All(Contains);
    
    /// <summary>
    /// Gets the permissions from the required set that are missing in this set.
    /// </summary>
    /// <param name="requiredPermissions">The permissions that are required</param>
    /// <returns>The permissions that are missing from this set</returns>
    public IEnumerable<string> GetMissingPermissions(IEnumerable<string> requiredPermissions) =>
        requiredPermissions.Where(p => !Contains(p));
    
    /// <summary>
    /// Performs a union operation (join in lattice theory).
    /// Returns a new permission set containing all permissions from both sets.
    /// </summary>
    /// <param name="other">The other permission set to union with</param>
    /// <returns>A new permission set containing the union of both sets</returns>
    public PermissionSet Union(PermissionSet other)
    {
        return new PermissionSet(_permissions.Union(other._permissions));
    }
    
    /// <summary>
    /// Performs an intersection operation (meet in lattice theory).
    /// Returns a new permission set containing only permissions present in both sets.
    /// </summary>
    /// <param name="other">The other permission set to intersect with</param>
    /// <returns>A new permission set containing the intersection of both sets</returns>
    public PermissionSet Intersect(PermissionSet other)
    {
        return new PermissionSet(_permissions.Intersect(other._permissions));
    }
    
    /// <summary>
    /// Checks if this permission set is a subset of another permission set.
    /// </summary>
    /// <param name="other">The other permission set to compare against</param>
    /// <returns>True if this set is a subset of the other set, false otherwise</returns>
    public bool IsSubsetOf(PermissionSet other)
    {
        return _permissions.IsSubsetOf(other._permissions);
    }
    
    /// <summary>
    /// Adds a permission to this set, returning a new permission set.
    /// </summary>
    /// <param name="permission">The permission to add</param>
    /// <returns>A new permission set with the added permission</returns>
    public PermissionSet Add(string permission)
    {
        return new PermissionSet(_permissions.Add(permission));
    }
    
    /// <summary>
    /// Adds multiple permissions to this set, returning a new permission set.
    /// </summary>
    /// <param name="permissions">The permissions to add</param>
    /// <returns>A new permission set with the added permissions</returns>
    public PermissionSet AddRange(IEnumerable<string> permissions)
    {
        return new PermissionSet(_permissions.Union(permissions));
    }
    
    /// <summary>
    /// Removes a permission from this set, returning a new permission set.
    /// </summary>
    /// <param name="permission">The permission to remove</param>
    /// <returns>A new permission set with the permission removed</returns>
    public PermissionSet Remove(string permission)
    {
        return new PermissionSet(_permissions.Remove(permission));
    }
    
    public bool Equals(PermissionSet? other)
    {
        return other is not null && _permissions.SetEquals(other._permissions);
    }
    
    public override bool Equals(object? obj) => obj is PermissionSet other && Equals(other);
    
    public override int GetHashCode()
    {
        return _permissions.Aggregate(0, (acc, permission) => HashCode.Combine(acc, permission.GetHashCode()));
    }
    
    public override string ToString()
    {
        return $"[{string.Join(", ", _permissions.OrderBy(p => p))}]";
    }
    
    public static bool operator ==(PermissionSet? left, PermissionSet? right) => 
        ReferenceEquals(left, right) || (left?.Equals(right) ?? false);
    
    public static bool operator !=(PermissionSet? left, PermissionSet? right) => !(left == right);
}