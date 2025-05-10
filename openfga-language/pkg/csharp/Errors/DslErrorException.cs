using System;
using System.Collections.Generic;

namespace OpenFga.Language.Errors;

public class DslErrorsException<TError>(List<TError> errors) : Exception(ErrorsWrapper<TError>.MessagesFromErrors(errors))
where TError : ParsingError
{
    public readonly List<TError> Errors = errors;
}