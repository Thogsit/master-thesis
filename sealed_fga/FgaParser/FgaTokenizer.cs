using System.Text.RegularExpressions;

namespace FgaParser;

public static class FgaTokenizer
{
    private static readonly Regex FgaTokenRegex = new(
        """
        (?<Model>model)|
        (?<Schema>schema)|
        (?<Type>type)|
        (?<Relations>relations)|
        (?<Define>define)|
        (?<Or>or)|
        (?<DefineIdentifier>[a-zA-Z_][a-zA-Z0-9_-]*)|
        (?<Number>\d+\.\d+)|
        (?<BracketLeft>\[)|
        (?<BracketRight>\])|
        (?<Hash>\#)|
        (?<Colon>:)|
        (?<Whitespace>\s+)
        """, RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

    public static List<FgaToken> Tokenize(string input)
    {
        var tokens = new List<FgaToken>();
        var matches = FgaTokenRegex.Matches(input);

        // Skip first group "0" which is the entire match
        var groupNames = FgaTokenRegex.GetGroupNames().Skip(1).ToArray();
        foreach (Match match in matches)
        {
            if (match.Groups["Whitespace"].Success)
                continue;

            foreach (var groupName in groupNames)
            {
                if (!match.Groups[groupName].Success)
                {
                    continue;
                }

                tokens.Add(new FgaToken(groupName, match.Value));
                break;
            }
        }

        return tokens;
    }
}

public class FgaToken(string type, string value)
{
    public string Type { get; } = type;
    public string Value { get; } = value;
}