namespace SealedFga;

/// <summary>
///     Configuration options for SealedFGA.
/// </summary>
public class SealedFgaOptions {
    /// <summary>
    ///     Gets or sets whether to use TickerQ for background processing of FGA service operations.
    ///     When true (default), operations are queued using TickerQ with retry mechanisms.
    ///     When false, operations are executed immediately in background using Task.Run().
    /// </summary>
    public bool QueueFgaServiceOperations { get; set; } = true;
}