using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace SealedFga.Database;

/// <summary>
///     Runs periodically to watch for FGA operations to send to the OpenFGA service.
///     Retries them with exponential backoff if any fail.
/// </summary>
public class FgaQueueProcessor : IDisposable {
    private Timer? _timer;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    /// <summary>
    ///     Starts the periodic processing of the FGA queue.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the method is called while the processing is already active.
    /// </exception>
    public void Start() {
        if (_timer != null) {
            throw new InvalidOperationException("Queue processing is already started");
        }

        _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    /// <summary>
    ///     Stops the periodic processing of the FGA queue.
    /// </summary>
    public void Stop() {
        _timer?.Dispose();
        _timer = null;
        _cancellationTokenSource.Cancel();

        try {
            _processingLock.Wait(TimeSpan.FromSeconds(5));
        } catch {
            // We wanna stop anyway
        } finally {
            _processingLock.Release();
        }
    }

    /// <summary>
    ///     The periodic callback that processes the FGA queue.
    ///     This is only a helper method to catch exceptions instead of crashing.
    /// </summary>
    private async void TimerCallback(object? _) {
        try {
            if (!await _processingLock.WaitAsync(0)) {
                return;
            }

            try {
                await ProcessBufferedFgaOperations(_cancellationTokenSource.Token);
            } catch (Exception) {
                // TODO: Implement proper interval check exception handling
            } finally {
                _processingLock.Release();
            }
        } catch (Exception) {
            // TODO: Check whether this should be better handled
        }
    }

    /// <summary>
    ///     Processes all pending FGA operations in the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop processing.</param>
    private async Task ProcessBufferedFgaOperations(CancellationToken cancellationToken) {
        using var connection = SealedFgaDb.Instance.OpenConnection();

        const string selectPendingOperationsSql = $"""
                                                   SELECT id, operation_type, user_val, relation_val, object_val, attempt_count, last_error
                                                   FROM fga_queue
                                                   WHERE status = '{FgaOperationStatus.Pending}'
                                                   AND next_retry_at <= strftime('%Y-%m-%d %H:%M:%f', 'now')
                                                   ORDER BY created_at ASC
                                                   LIMIT 100
                                                   """;

        using var selectCmd = new SqliteCommand(selectPendingOperationsSql, connection);
        using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken);

        var pendingOperations = new List<OpenFgaQueueEntry>();
        while (await reader.ReadAsync(cancellationToken)) {
            pendingOperations.Add(new OpenFgaQueueEntry(
                    Id: reader.GetInt64(0),
                    OperationType: reader.GetString(1),
                    User: reader.GetString(2),
                    Relation: reader.GetString(3),
                    Object: reader.GetString(4),
                    AttemptCount: reader.GetInt32(5),
                    LastError: reader.IsDBNull(6) ? null : reader.GetString(6)
                )
            );
        }

        foreach (var operation in pendingOperations) {
            try {
                // TODO: Send to OpenFGA service
                // This section will handle the actual FGA API calls based on operation.OperationType
                // - For Write operations: call FGA write API with user, relation, object
                // - For Delete operations: call FGA delete API with user, relation, object
                // - For Read operations: call FGA read API with user, relation, object

                await MarkOperationAsSuccessful(operation.Id, cancellationToken);
            } catch (Exception ex) {
                await MarkOperationAsFailed(operation.Id, operation.AttemptCount + 1, ex.Message, cancellationToken);
            }
        }
    }

    /// <summary>
    ///     Marks the given operation as successful in the database.
    /// </summary>
    /// <param name="operationId">ID of the operation to mark as successful.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    private async Task MarkOperationAsSuccessful(long operationId, CancellationToken cancellationToken) {
        using var connection = SealedFgaDb.Instance.OpenConnection();

        const string updateSql = $"""
                                  UPDATE fga_queue
                                  SET status = '{FgaOperationStatus.Success}', last_error = NULL
                                  WHERE id = @id
                                  """;

        using var cmd = new SqliteCommand(updateSql, connection);
        cmd.Parameters.AddWithValue("@id", operationId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    ///     Marks a specific FGA operation as failed, updating its status, retry attempt count,
    ///     and scheduling the next retry attempt if applicable.
    /// </summary>
    /// <param name="operationId">The ID of the operation to be marked as failed.</param>
    /// <param name="newAttemptCount">The updated number of attempts made for the operation.</param>
    /// <param name="errorMessage">The error message describing the failure reason.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task MarkOperationAsFailed(
        long operationId,
        int newAttemptCount,
        string errorMessage,
        CancellationToken cancellationToken
    ) {
        using var connection = SealedFgaDb.Instance.OpenConnection();

        // Calculate next retry time with exponential backoff
        var nextRetryDelayMinutes = Math.Min(Math.Pow(2, newAttemptCount - 1), 60); // Cap at 60 minutes
        var nextRetryAt = DateTime.UtcNow.AddMinutes(nextRetryDelayMinutes);

        // Mark as failure if too many attempts
        var status = newAttemptCount >= 5 ? FgaOperationStatus.Failure : FgaOperationStatus.Pending;

        const string updateSql = """
                                 UPDATE fga_queue
                                 SET status = @status,
                                     attempt_count = @attemptCount,
                                     last_error = @lastError,
                                     next_retry_at = @nextRetryAt
                                 WHERE id = @id
                                 """;

        using var cmd = new SqliteCommand(updateSql, connection);
        cmd.Parameters.AddWithValue("@id", operationId);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@attemptCount", newAttemptCount);
        cmd.Parameters.AddWithValue("@lastError", errorMessage);
        cmd.Parameters.AddWithValue("@nextRetryAt", nextRetryAt.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose() {
        Stop();
        _processingLock?.Dispose();
        _cancellationTokenSource.Dispose();
        GC.SuppressFinalize(this);
    }
}
