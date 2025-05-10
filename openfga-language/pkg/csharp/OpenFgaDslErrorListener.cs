using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using OpenFga.Language.Errors;

namespace OpenFga.Language;

public class OpenFgaDslErrorListener<TSymbol> : IAntlrErrorListener<TSymbol>
{
    public readonly List<SyntaxError> Errors = [];

    // line is one-based, i.e., the first line will be 1
    // column is zero-based, i.e., the first column will be 0
    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        TSymbol? offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e
    )
    {
        ErrorMetadata? errorMetadata = null;
        var columnOffset = 0;
        if (offendingSymbol != null)
        {
            errorMetadata = new ErrorMetadata(offendingSymbol.ToString());
            columnOffset += offendingSymbol.ToString().Length;
        }
        
        var properties = new ErrorProperties(
            new StartEnd(line - 1, line - 1),
            new StartEnd(charPositionInLine, charPositionInLine + columnOffset),
            msg
        );
        
        Errors.Add(new SyntaxError(properties, e, errorMetadata));
    }
}