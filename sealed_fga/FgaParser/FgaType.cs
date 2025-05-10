namespace FgaParser;

public class FgaType(string name)
{
    public string Name { get; } = name;
    public List<FgaRelation> Relations { get; } = [];
}