using System.Collections.Generic;
using OpenFga.Language.Model;

namespace OpenFga.Language;

public class StackRelation(List<Userset> rewrites, string operatorType)
{
    public readonly List<Userset> Rewrites = rewrites;
    public readonly string Operator = operatorType;
}