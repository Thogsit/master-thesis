using System;

namespace SealedFga.Attributes;

public enum SealedFgaRelationTargetType {
    User,
    Object,
}

[AttributeUsage(AttributeTargets.Property)]
public class SealedFgaRelationAttribute(
    string relation,
    SealedFgaRelationTargetType targetType = SealedFgaRelationTargetType.Object) : Attribute {
    public string Relation { get; } = relation;
    public SealedFgaRelationTargetType TargetType { get; } = targetType;
}
