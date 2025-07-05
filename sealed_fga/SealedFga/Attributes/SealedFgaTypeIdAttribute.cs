using System;
using SealedFga.Models;

namespace SealedFga.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class SealedFgaTypeIdAttribute(string name, SealedFgaTypeIdType type) : Attribute {
    public string Name { get; } = name;
    public SealedFgaTypeIdType Type { get; } = type;
}
