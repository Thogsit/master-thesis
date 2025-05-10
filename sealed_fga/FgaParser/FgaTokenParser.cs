namespace FgaParser;

public class FgaTokenParser(List<FgaToken> tokens)
{
    private int _position;

    private FgaToken? CurrentToken => _position < tokens.Count ? tokens[_position] : null;

    public FgaModel Parse()
    {
        var model = new FgaModel();
        ParseModel(model);
        return model;
    }

    private void ParseModel(FgaModel model)
    {
        if (CurrentToken?.Type != "Model") throw new Exception("Expected 'model' keyword");
        _position++;

        if (CurrentToken?.Type == "Schema")
        {
            _position++; // skip schema keyword
            var schemaVersion = CurrentToken?.Value!;
            model.SchemaVersion = schemaVersion;
            _position++; // skip version number
        }

        while (CurrentToken?.Type == "Type")
        {
            var type = ParseType();
            model.Types.Add(type);
        }
    }

    private FgaType ParseType()
    {
        if (CurrentToken?.Type != "Type") throw new Exception("Expected 'type' keyword");
        _position++; // skip type keyword

        var typeName = CurrentToken?.Value!;
        _position++; // skip type name

        var fgaType = new FgaType(typeName);
        ParseRelations(fgaType);
        return fgaType;
    }

    private void ParseRelations(FgaType fgaType)
    {
        if (CurrentToken?.Type != "Relations") return;

        _position++; // skip relations keyword

        while (CurrentToken?.Type == "Define")
        {
            var relation = ParseRelation();
            fgaType.Relations.Add(relation);
        }
    }

    private FgaRelation ParseRelation()
    {
        if (CurrentToken?.Type != "Define") throw new Exception("Expected 'define' keyword");
        _position++; // skip define keyword

        var relationName = CurrentToken?.Value!;
        _position++; // skip relation name

        if (CurrentToken?.Type != "Colon") throw new Exception("Expected ':' after relation name");
        _position++; // skip colon

        var targets = ParseRelationTargets();
        return new FgaRelation(relationName, targets);
    }

    private List<string> ParseRelationTargets()
    {
        var targets = new List<string>();

        if (CurrentToken?.Type == "BracketLeft")
        {
            _position++; // skip bracket

            while (CurrentToken?.Type is "DefineIdentifier" or "Or")
            {
                targets.Add(CurrentToken?.Value!);
                _position++; // skip target name or "or"

                if (CurrentToken?.Type == "Or")
                    _position++; // skip "or"
            }

            if (CurrentToken?.Type != "BracketRight") throw new Exception("Expected ']' at end of targets");
            _position++; // skip bracket
        }

        return targets;
    }
}