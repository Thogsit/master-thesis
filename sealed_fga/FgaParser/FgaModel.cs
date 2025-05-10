namespace FgaParser;

public class FgaModel
{
    public string SchemaVersion { get; set; }
    public List<FgaType> Types { get; } = [];
}
