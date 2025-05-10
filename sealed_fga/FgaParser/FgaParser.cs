namespace FgaParser;

public static class FgaParser
{
    public static FgaModel ParseFromContent(string fgaFileContent)
    {
        var fgaTokens = FgaTokenizer.Tokenize(fgaFileContent);
        var parser = new FgaTokenParser(fgaTokens);
        var fgaModel = parser.Parse();

        return fgaModel;
    }
}
