using System;
using SealedFga.Models;

namespace SealedFga.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class OpenFgaTypeIdAttribute(string name, OpenFgaTypeIdType type) : Attribute
{
    public string Name { get; } = name;
    public OpenFgaTypeIdType Type { get; } = type;
}
