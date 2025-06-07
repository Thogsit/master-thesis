using System;

namespace SealedFga.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class ImplementedByAttribute(Type implementingClass) : Attribute {
    public Type ImplementingClass { get; } = implementingClass;
}
