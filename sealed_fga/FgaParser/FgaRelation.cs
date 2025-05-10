namespace FgaParser;

public class FgaRelation(string name, List<string> targets)
{
    public string Name { get; } = name;
    public List<string> Targets { get; } = targets;
}