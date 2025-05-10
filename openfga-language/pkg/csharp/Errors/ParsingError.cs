namespace OpenFga.Language.Errors;

public abstract class ParsingError(ErrorType type, ErrorProperties properties) : SimpleError(properties.Message)
{
    public readonly StartEnd? Line = properties.Line;
    public readonly StartEnd? Column = properties.Column;
    public readonly string FullMessage = properties.GetFullMessage(type);

    public StartEnd GetLineWithOffset(int offset) => Line!.WithOffset(offset);
    public StartEnd GetColumnWithOffset(int offset) => Column!.WithOffset(offset);

    public override string ToString()
    {
        return FullMessage;
    }
}