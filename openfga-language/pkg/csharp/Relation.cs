using System.Collections.Generic;
using OpenFga.Language.Model;

namespace OpenFga.Language
{
    public sealed class Relation(string? name, List<Userset> rewrites, string? operatorType, RelationMetadata typeInfo)
    {
        public readonly string? Name = name;
        public List<Userset> Rewrites = rewrites;
        public string? Operator = operatorType;
        public RelationMetadata TypeInfo = typeInfo;
    }
}