using System.Collections.Generic;
using System.Linq;

namespace OpenFga.Language.Errors;

public abstract class ErrorsWrapper<T>(List<T> errors) : SimpleError(MessagesFromErrors(errors))
    where T : notnull
{
    private const string Delimiter = "\n\t* ";
    private const string Suffix = "\n\n";

    public List<T> Errors { get; } = errors;

    public static string MessagesFromErrors(List<T> errors)
    {
        var errorsPlural = errors.Count > 1 ? "s" : "";
        var prefix = $"{errors.Count} error{errorsPlural} occurred:{Delimiter}";
        return errors.Select(e => e.ToString()).Aggregate(prefix, (current, error) => current + error + Delimiter)
            .TrimEnd(Delimiter.ToCharArray()) + Suffix;
    }
}