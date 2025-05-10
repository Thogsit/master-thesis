using Antlr4.Runtime;

namespace OpenFga.Language.Errors;

public class SyntaxError(ErrorProperties properties, RecognitionException cause, ErrorMetadata? metadata)
    : ParsingError(ErrorType.Syntax, properties)
{
    public readonly ErrorMetadata? Metadata = metadata;
    public readonly RecognitionException Cause = cause;
}