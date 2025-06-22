using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using SealedFga.Database;
using Tuple = OpenFga.Sdk.Model.Tuple;

namespace SealedFga.Sample.OpenFga;

/// <summary>
///     Wrapper class for communicating with the OpenFGA service using strongly typed IDs.
///     Handles reads directly; queues writes/deletes for reliable processing.
/// </summary>
public class SealedFgaService : IDisposable {
    private readonly OpenFgaClient _client;
    private readonly FgaQueueProcessor _queueProcessor;

    public SealedFgaService(OpenFgaClient client) {
        _client = client;
        _queueProcessor = new FgaQueueProcessor();
        _queueProcessor.Start();
    }

    #region Strongly-Typed ID Methods

    /// <summary>
    ///     Checks authorization using strongly typed IDs.
    /// </summary>
    /// <param name="user">The user ID (strongly typed)</param>
    /// <param name="relation">The relation string</param>
    /// <param name="objectId">The object ID (strongly typed)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TUserId">The user ID type</typeparam>
    /// <typeparam name="TObjId">The object ID type</typeparam>
    /// <returns>True if the relation exists, false otherwise</returns>
    public async Task<bool> CheckAsync<TUserId, TObjId>(
        TUserId user,
        IOpenFgaRelation<TObjId> relation,
        TObjId objectId,
        CancellationToken cancellationToken = new()
    )
        where TUserId : IOpenFgaTypeId<TUserId>
        where TObjId : IOpenFgaTypeId<TObjId>
        => await CheckAsync(
            user.AsOpenFgaIdTupleString(),
            relation.AsOpenFgaString(),
            objectId.AsOpenFgaIdTupleString(),
            cancellationToken
        );

    /// <summary>
    ///     Queues a write operation using strongly typed IDs.
    /// </summary>
    /// <param name="user">The user ID (strongly typed)</param>
    /// <param name="relation">The relation string</param>
    /// <param name="objectId">The object ID (strongly typed)</param>
    /// <typeparam name="TUserId">The user ID type</typeparam>
    /// <typeparam name="TObjId">The object ID type</typeparam>
    public void QueueWrite<TUserId, TObjId>(
        TUserId user,
        IOpenFgaRelation<TObjId> relation,
        TObjId objectId
    )
        where TUserId : IOpenFgaTypeId<TUserId>
        where TObjId : IOpenFgaTypeId<TObjId>
        => QueueWrite(
            user.AsOpenFgaIdTupleString(),
            relation.AsOpenFgaString(),
            objectId.AsOpenFgaIdTupleString()
        );

    /// <summary>
    ///     Queues a delete operation using strongly typed IDs.
    /// </summary>
    /// <param name="user">The user ID (strongly typed)</param>
    /// <param name="relation">The relation string</param>
    /// <param name="objectId">The object ID (strongly typed)</param>
    /// <typeparam name="TUserId">The user ID type</typeparam>
    /// <typeparam name="TObjId">The object ID type</typeparam>
    public void QueueDelete<TUserId, TObjId>(
        TUserId user,
        IOpenFgaRelation<TObjId> relation,
        TObjId objectId
    )
        where TUserId : IOpenFgaTypeId<TUserId>
        where TObjId : IOpenFgaTypeId<TObjId>
        => QueueDelete(
            user.AsOpenFgaIdTupleString(),
            relation.AsOpenFgaString(),
            objectId.AsOpenFgaIdTupleString()
        );

    /// <summary>
    ///     Queues multiple write operations using strongly typed IDs.
    /// </summary>
    /// <param name="operations">Collection of write operations with strongly typed IDs</param>
    /// <typeparam name="TUserId">The user ID type</typeparam>
    /// <typeparam name="TObjId">The object ID type</typeparam>
    public void QueueWrites<TUserId, TObjId>(
        IEnumerable<(TUserId User, IOpenFgaRelation<TObjId> Relation, TObjId Object)> operations
    )
        where TUserId : IOpenFgaTypeId<TUserId>
        where TObjId : IOpenFgaTypeId<TObjId>
        => QueueWrites(
            operations.Select(op => (
                    op.User.AsOpenFgaIdTupleString(),
                    op.Relation.AsOpenFgaString(),
                    op.Object.AsOpenFgaIdTupleString()
                )
            )
        );

    /// <summary>
    ///     Queues multiple delete operations using strongly typed IDs.
    /// </summary>
    /// <param name="operations">Collection of delete operations with strongly typed IDs</param>
    /// <typeparam name="TUserId">The user ID type</typeparam>
    /// <typeparam name="TObjId">The object ID type</typeparam>
    public void QueueDeletes<TUserId, TObjId>(
        IEnumerable<(TUserId User, IOpenFgaRelation<TObjId> Relation, TObjId Object)> operations
    )
        where TUserId : IOpenFgaTypeId<TUserId>
        where TObjId : IOpenFgaTypeId<TObjId>
        => QueueDeletes(
            operations.Select(op => (
                    op.User.AsOpenFgaIdTupleString(),
                    op.Relation.AsOpenFgaString(),
                    op.Object.AsOpenFgaIdTupleString()
                )
            )
        );

    /// <summary>
    ///     Lists objects that a user has a specific relation to, returning strongly typed IDs.
    /// </summary>
    /// <param name="user">The user to check (strongly typed)</param>
    /// <param name="relation">The relation to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TUserId">The user ID type</typeparam>
    /// <typeparam name="TObjId">The object ID type</typeparam>
    /// <returns>List of strongly typed object IDs</returns>
    public async Task<IEnumerable<TObjId>> ListObjectsAsync<TUserId, TObjId>(
        TUserId user,
        IOpenFgaRelation<TObjId> relation,
        CancellationToken cancellationToken = new()
    )
        where TUserId : IOpenFgaTypeId<TUserId>
        where TObjId : IOpenFgaTypeId<TObjId> {
        var objectStrings = await ListObjectsAsync(
            user.AsOpenFgaIdTupleString(),
            relation.AsOpenFgaString(),
            TObjId.OpenFgaTypeName,
            cancellationToken
        );

        return objectStrings.Select(TObjId.Parse);
    }

    /// <summary>
    ///     Performs batch check operations using strongly typed IDs.
    /// </summary>
    /// <param name="checks">List of check requests with strongly typed IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TUserId">The user ID type</typeparam>
    /// <typeparam name="TObjId">The object ID type</typeparam>
    /// <returns>Dictionary with results for each check</returns>
    public async Task<Dictionary<(TUserId User, IOpenFgaRelation<TObjId> Relation, TObjId Object), bool>>
        BatchCheckAsync<TUserId, TObjId>(
            IEnumerable<(TUserId User, IOpenFgaRelation<TObjId> Relation, TObjId Object)> checks,
            CancellationToken cancellationToken = new()
        )
        where TUserId : IOpenFgaTypeId<TUserId>
        where TObjId : IOpenFgaTypeId<TObjId> {
        var checksAsList = checks.ToList();
        var results = await BatchCheckAsync(
            checksAsList.Select(check => (
                    check.User.AsOpenFgaIdTupleString(),
                    check.Relation.AsOpenFgaString(),
                    check.Object.AsOpenFgaIdTupleString()
                )
            ),
            cancellationToken
        );

        return checksAsList.ToDictionary(
            check => (check.User, check.Relation, check.Object),
            check => results.ElementAt(checksAsList.IndexOf(check))
        );
    }

    #endregion

    #region Raw String Methods (for backwards compatibility and edge cases)

    /// <summary>
    ///     Reads tuples from OpenFGA directly using raw strings (not queued).
    /// </summary>
    /// <param name="user">Optional user filter</param>
    /// <param name="relation">Optional relation filter</param>
    /// <param name="objectId">Optional object filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tuples matching the criteria</returns>
    internal async Task<IEnumerable<Tuple>> ReadAsync(
        string? user = null,
        string? relation = null,
        string? objectId = null,
        CancellationToken cancellationToken = new()
    ) {
        var request = new ClientReadRequest {
            User = user,
            Relation = relation,
            Object = objectId,
        };

        var response = await _client.Read(request, cancellationToken: cancellationToken);
        return response.Tuples;
    }

    /// <summary>
    ///     Checks authorization using raw strings.
    /// </summary>
    /// <param name="user">The user string</param>
    /// <param name="relation">The relation string</param>
    /// <param name="objectId">The object string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the relation exists, false otherwise</returns>
    internal async Task<bool> CheckAsync(
        string user,
        string relation,
        string objectId,
        CancellationToken cancellationToken = new()
    ) {
        var response = await _client.Check(new ClientCheckRequest {
                User = user,
                Relation = relation,
                Object = objectId,
            },
            cancellationToken: cancellationToken
        );

        return response.Allowed ?? false;
    }

    /// <summary>
    ///     Queues a write operation using raw strings.
    /// </summary>
    /// <param name="user">The user string</param>
    /// <param name="relation">The relation string</param>
    /// <param name="objectId">The object string</param>
    internal void QueueWrite(string user, string relation, string objectId) {
        var operation = new FgaOperation(
            FgaOperationType.Write,
            user,
            relation,
            objectId
        );

        SealedFgaDb.Instance.AddFgaOperation(operation);
    }

    /// <summary>
    ///     Queues a delete operation using raw strings.
    /// </summary>
    /// <param name="user">The user string</param>
    /// <param name="relation">The relation string</param>
    /// <param name="objectId">The object string</param>
    internal void QueueDelete(string user, string relation, string objectId) {
        var operation = new FgaOperation(
            FgaOperationType.Delete,
            user,
            relation,
            objectId
        );

        SealedFgaDb.Instance.AddFgaOperation(operation);
    }

    /// <summary>
    ///     Queues multiple write operations using raw strings.
    /// </summary>
    /// <param name="tuples">Collection of write operations with raw strings</param>
    internal void QueueWrites(IEnumerable<(string User, string Relation, string Object)> tuples) {
        var operations = new List<FgaOperation>();

        foreach (var (user, relation, objectId) in tuples) {
            operations.Add(new FgaOperation(
                    FgaOperationType.Write,
                    user,
                    relation,
                    objectId
                )
            );
        }

        SealedFgaDb.Instance.AddFgaOperations(operations);
    }

    /// <summary>
    ///     Queues multiple delete operations using raw strings.
    /// </summary>
    /// <param name="tuples">Collection of delete operations with raw strings</param>
    internal void QueueDeletes(IEnumerable<(string User, string Relation, string Object)> tuples) {
        var operations = new List<FgaOperation>();

        foreach (var (user, relation, objectId) in tuples) {
            operations.Add(new FgaOperation(
                    FgaOperationType.Delete,
                    user,
                    relation,
                    objectId
                )
            );
        }

        SealedFgaDb.Instance.AddFgaOperations(operations);
    }

    /// <summary>
    ///     Lists objects that a user has a specific relation to using raw strings.
    /// </summary>
    /// <param name="user">The user string</param>
    /// <param name="relation">The relation string</param>
    /// <param name="objectType">The type of objects to list</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of object strings</returns>
    internal async Task<IEnumerable<string>> ListObjectsAsync(
        string user,
        string relation,
        string objectType,
        CancellationToken cancellationToken = new()
    ) {
        var response = await _client.ListObjects(new ClientListObjectsRequest {
                User = user,
                Relation = relation,
                Type = objectType,
            },
            cancellationToken: cancellationToken
        );

        return response.Objects;
    }

    /// <summary>
    ///     Performs batch check operations using raw strings.
    /// </summary>
    /// <param name="checks">List of check requests with raw strings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Results in the same order as input checks</returns>
    internal async Task<IEnumerable<bool>> BatchCheckAsync(
        IEnumerable<(string User, string Relation, string Object)> checks,
        CancellationToken cancellationToken = new()
    ) {
        // TODO: OpenFGA .NET SDK does not support batch check operations directly. Switch to them when available.
        var checkTasks = checks.Select(async check => {
                try {
                    return await CheckAsync(check.User, check.Relation, check.Object, cancellationToken);
                } catch (Exception) {
                    return false;
                }
            }
        );

        return await Task.WhenAll(checkTasks);
    }

    #endregion

    /// <summary>
    ///     Disposes the wrapper and stops the queue processor.
    /// </summary>
    public void Dispose() {
        _queueProcessor.Dispose();
        GC.SuppressFinalize(this);
    }
}
