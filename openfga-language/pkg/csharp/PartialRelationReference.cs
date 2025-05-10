using OpenFga.Language.Model;

namespace OpenFga.Language;

public class PartialRelationReference(string? type, string? relation, object? wildcard, string? condition)
{
    public readonly string? Type = type;
    public readonly string? Relation = relation;
    public readonly object? Wildcard = wildcard;
    public readonly string? Condition = condition;

    public RelationReference AsRelationReference()
    {
        return new RelationReference(Type, Relation, Wildcard, Condition);
    }
}
