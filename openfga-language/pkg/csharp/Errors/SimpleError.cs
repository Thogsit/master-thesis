using System.Text.Json.Serialization;

namespace OpenFga.Language.Errors;

public abstract class SimpleError(string message)
{
    [JsonPropertyName("msg")]
    public readonly string Message = message;

    public override string ToString()
    {
        return Message;
    }
}