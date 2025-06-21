namespace SealedFga.Database;

/// <summary>
///     Represents an FGA operation that should be sent to the OpenFGA service.
///     Can be used to write or delete FGA tuples.
/// </summary>
/// <param name="OperationType">Defines the type of the operation, see <see cref="FgaOperationType"/>.</param>
/// <param name="RawUser">The raw FGA user string.</param>
/// <param name="Relation">The raw relationship name.</param>
/// <param name="RawObject">The raw FGA object string.</param>
public record FgaOperation(
    string OperationType,
    string RawUser,
    string Relation,
    string RawObject
);
