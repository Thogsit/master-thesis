namespace OpenFga.Language.Errors;

public class ErrorType
{
    public static readonly ErrorType Syntax = new("syntax");
    public static readonly ErrorType Validation = new("validation");

    public string Value { get; }

    private ErrorType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }
}