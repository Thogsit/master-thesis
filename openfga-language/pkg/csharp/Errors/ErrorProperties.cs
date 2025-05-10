namespace OpenFga.Language.Errors;

public class ErrorProperties(StartEnd? line, StartEnd? column, string message)
{
    public readonly StartEnd? Line = line;
    public readonly StartEnd? Column = column;
    public readonly string Message = message;

    public string GetFullMessage(ErrorType type)
    {
        if (Line != null && Column != null)
        {
            return $"{type} error at line={Line.Start}, column={Column.Start}: {Message}";
        }

        return $"{type} error: {Message}";
    }
}