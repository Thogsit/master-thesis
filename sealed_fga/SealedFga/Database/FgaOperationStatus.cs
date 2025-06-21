namespace SealedFga.Database;

/// <summary>
///     The current status of a buffered FGA operation.
/// </summary>
public static class FgaOperationStatus {
    /// <summary>
    ///     Sent successful.
    /// </summary>
    public const string Success = "success";

    /// <summary>
    ///     Terminal failure, no more retries will be attempted.
    /// </summary>
    public const string Failure = "failure";

    /// <summary>
    ///     Still pending, will be retried later.
    /// </summary>
    public const string Pending = "pending";
}
