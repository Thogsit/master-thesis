using System;

namespace SealedFga.Attributes;

public enum OpenFgaRelationTargetType {
    User,
    Object,
}

[AttributeUsage(AttributeTargets.Property)]
public class OpenFgaRelationAttribute(
    string relation,
    OpenFgaRelationTargetType targetType = OpenFgaRelationTargetType.Object) : Attribute {
    public string Relation { get; } = relation;
    public OpenFgaRelationTargetType TargetType { get; } = targetType;
}
