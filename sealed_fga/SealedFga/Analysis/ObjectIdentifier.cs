using System;
using Microsoft.CodeAnalysis;

namespace SealedFga.Analysis;

/// <summary>
/// Abstract base class for identifying objects in the data flow analysis.
/// Provides type-safe object tracking across different contexts.
/// </summary>
public abstract record ObjectIdentifier : IEquatable<ObjectIdentifier>
{
    /// <summary>
    /// Represents a reference to a variable in the current scope.
    /// </summary>
    /// <param name="Type">The type symbol of the variable</param>
    public sealed record VariableReference(ITypeSymbol Type) : ObjectIdentifier
    {
        public override string ToString() => $"var:{Type.Name}";
        
        public bool Equals(VariableReference? other) => 
            other is not null && 
            SymbolEqualityComparer.Default.Equals(Type, other.Type);
        
        public override int GetHashCode() => 
            SymbolEqualityComparer.Default.GetHashCode(Type);
    }
    
    /// <summary>
    /// Represents a specific entity ID with its type information.
    /// </summary>
    /// <param name="EntityType">The type name of the entity (e.g., "Secret", "Document")</param>
    /// <param name="IdValue">The string representation of the ID value</param>
    public sealed record EntityId(string EntityType, string IdValue) : ObjectIdentifier
    {
        public override string ToString() => $"{EntityType}:{IdValue}";
    }
    
    /// <summary>
    /// Represents a reference to a method parameter.
    /// </summary>
    /// <param name="Type">The type symbol of the parameter</param>
    public sealed record ParameterReference(ITypeSymbol Type) : ObjectIdentifier
    {
        public override string ToString() => $"param:{Type.Name}";
        
        public bool Equals(ParameterReference? other) => 
            other is not null && 
            SymbolEqualityComparer.Default.Equals(Type, other.Type);
        
        public override int GetHashCode() => 
            SymbolEqualityComparer.Default.GetHashCode(Type);
    }
    
    /// <summary>
    /// Represents access to a property of another object.
    /// </summary>
    /// <param name="Base">The base object being accessed</param>
    /// <param name="PropertyName">The name of the property</param>
    public sealed record PropertyAccess(ObjectIdentifier Base, string PropertyName) : ObjectIdentifier
    {
        public override string ToString() => $"{Base}.{PropertyName}";
    }
}