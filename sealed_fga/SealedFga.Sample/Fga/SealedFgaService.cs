using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using SealedFga.Sample.Fga.Payloads;
using TickerQ.Utilities;
using TickerQ.Utilities.Interfaces.Managers;
using TickerQ.Utilities.Models.Ticker;
using Tuple = OpenFga.Sdk.Model.Tuple;

namespace SealedFga.Sample.Fga;

/// <summary>
///     Wrapper class for communicating with the OpenFGA service using strongly typed IDs.
///     Handles reads directly; queues writes/deletes for reliable processing.
/// </summary>
public class SealedFgaService(
    OpenFgaClient openFgaClient,
    ITimeTickerManager<TimeTicker> tickerQ
) {
    public const int DefaultRetryCount = 5;

    public static readonly int[] DefaultRetryIntervals = [
        TimeSpan.FromSeconds(0).Seconds,
        TimeSpan.FromMinutes(1).Seconds,
        TimeSpan.FromHours(1).Seconds,
        TimeSpan.FromHours(6).Seconds,
        TimeSpan.FromDays(1).Seconds,
    ];

    private TimeTicker CreateTimeTicker<TReq>(
        string functionName,
        TReq request,
        int retries = DefaultRetryCount,
        int[]? retryIntervals = null
    ) {
        retryIntervals ??= DefaultRetryIntervals;
        return new TimeTicker {
            Function = functionName,
            Request = TickerHelper.CreateTickerRequest(request),
            Retries = retries,
            ExecutionTime = DateTime.Now.AddSeconds(1),
            RetryIntervals = retryIntervals,
        };
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
    public async Task QueueWrite<TUserId, TObjId>(
        TUserId user,
        IOpenFgaRelation<TObjId> relation,
        TObjId objectId
    )
        where TUserId : IOpenFgaTypeId<TUserId>
        where TObjId : IOpenFgaTypeId<TObjId>
        => await QueueWrite(
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
    public async Task QueueDelete<TUserId, TObjId>(
        TUserId user,
        IOpenFgaRelation<TObjId> relation,
        TObjId objectId
    )
        where TUserId : IOpenFgaTypeId<TUserId>
        where TObjId : IOpenFgaTypeId<TObjId>
        => await QueueDelete(
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
    public async Task QueueWrites<TUserId, TObjId>(
        IEnumerable<(TUserId User, IOpenFgaRelation<TObjId> Relation, TObjId Object)> operations
    )
        where TUserId : IOpenFgaTypeId<TUserId>
        where TObjId : IOpenFgaTypeId<TObjId>
        => await QueueWrites(
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
    public async Task QueueDeletes<TUserId, TObjId>(
        IEnumerable<(TUserId User, IOpenFgaRelation<TObjId> Relation, TObjId Object)> operations
    )
        where TUserId : IOpenFgaTypeId<TUserId>
        where TObjId : IOpenFgaTypeId<TObjId>
        => await QueueDeletes(
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
    /// <param name="rawUser">Optional user filter</param>
    /// <param name="rawRelation">Optional relation filter</param>
    /// <param name="rawObject">Optional object filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tuples matching the criteria</returns>
    internal async Task<IEnumerable<Tuple>> ReadAsync(
        string? rawUser = null,
        string? rawRelation = null,
        string? rawObject = null,
        CancellationToken cancellationToken = new()
    ) {
        var request = new ClientReadRequest {
            User = rawUser,
            Relation = rawRelation,
            Object = rawObject,
        };

        var response = await openFgaClient.Read(request, cancellationToken: cancellationToken);
        return response.Tuples;
    }

    /// <summary>
    ///     Checks authorization using raw strings.
    /// </summary>
    /// <param name="rawUser">The user string</param>
    /// <param name="rawRelation">The relation string</param>
    /// <param name="rawObject">The object string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the relation exists, false otherwise</returns>
    internal async Task<bool> CheckAsync(
        string rawUser,
        string rawRelation,
        string rawObject,
        CancellationToken cancellationToken = new()
    ) {
        var response = await openFgaClient.Check(new ClientCheckRequest {
                User = rawUser,
                Relation = rawRelation,
                Object = rawObject,
            },
            cancellationToken: cancellationToken
        );

        return response.Allowed ?? false;
    }

    /// <summary>
    ///     Queues a write operation using raw strings.
    /// </summary>
    /// <param name="rawUser">The user string</param>
    /// <param name="rawRelation">The relation string</param>
    /// <param name="rawObject">The object string</param>
    internal async Task QueueWrite(string rawUser, string rawRelation, string rawObject)
        => await tickerQ.AddAsync(
            CreateTimeTicker(
                FgaQueueHandlerService.FgaWriteJobName,
                new FgaQueueWritePayload {
                    RawUser = rawUser,
                    RawRelation = rawRelation,
                    RawObject = rawObject,
                }
            )
        );

    /// <summary>
    ///     Queues a delete operation using raw strings.
    /// </summary>
    /// <param name="user">The user string</param>
    /// <param name="relation">The relation string</param>
    /// <param name="objectId">The object string</param>
    internal async Task QueueDelete(string user, string relation, string objectId)
        => await tickerQ.AddAsync(
            CreateTimeTicker(
                FgaQueueHandlerService.FgaDeleteJobName,
                new FgaQueueDeletePayload {
                    RawUser = user,
                    RawRelation = relation,
                    RawObject = objectId,
                }
            )
        );

    /// <summary>
    ///     Queues multiple write operations using raw strings.
    /// </summary>
    /// <param name="tuples">Collection of write operations with raw strings</param>
    internal async Task QueueWrites(IEnumerable<(string User, string Relation, string Object)> tuples)
        => await tickerQ.AddAsync(
            CreateTimeTicker(
                FgaQueueHandlerService.FgaWriteMultipleJobName,
                tuples.Select(t => new FgaQueueWritePayload {
                        RawUser = t.User,
                        RawRelation = t.Relation,
                        RawObject = t.Object,
                    }
                ).ToList()
            )
        );

    /// <summary>
    ///     Queues multiple delete operations using raw strings.
    /// </summary>
    /// <param name="tuples">Collection of delete operations with raw strings</param>
    internal async Task QueueDeletes(IEnumerable<(string User, string Relation, string Object)> tuples)
        => await tickerQ.AddAsync(
            CreateTimeTicker(
                FgaQueueHandlerService.FgaDeleteMultipleJobName,
                tuples.Select(t => new FgaQueueDeletePayload {
                        RawUser = t.User,
                        RawRelation = t.Relation,
                        RawObject = t.Object,
                    }
                ).ToList()
            )
        );

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
        var response = await openFgaClient.ListObjects(new ClientListObjectsRequest {
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

    #region Write/Delete Methods

    /// <summary>
    ///     Safely writes a list of tuples to OpenFGA after checking if they don't already exist.
    ///     This prevents failures when attempting to write tuples that already exist.
    /// </summary>
    /// <param name="tuples">The list of tuples to write</param>
    /// <param name="ct">The cancellation token to cancel the operation if needed</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. Returns a list of booleans indicating which tuples were
    ///     written (true) or already existed (false).
    /// </returns>
    public async Task<List<bool>> SafeWriteTupleAsync(
        List<FgaQueueWritePayload> tuples,
        CancellationToken ct = new()
    ) {
        return await SafeTupleOperationAsync(
            tuples,
            tuple => (tuple.RawUser, tuple.RawRelation, tuple.RawObject),
            exists => !exists,
            tuplesToProcess => tuplesToProcess.Select(tuple => new ClientTupleKey {
                    User = tuple.RawUser,
                    Relation = tuple.RawRelation,
                    Object = tuple.RawObject,
                }
            ).ToList(),
            processedTuples => new ClientWriteRequest { Writes = processedTuples },
            ct
        );
    }

    /// <summary>
    ///     Safely deletes a list of tuples from OpenFGA after checking if they exist.
    ///     This prevents failures when attempting to delete tuples that don't exist.
    /// </summary>
    /// <param name="tuples">The list of tuples to delete</param>
    /// <param name="ct">The cancellation token to cancel the operation if needed</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. Returns a list of booleans indicating which tuples were
    ///     deleted (true) or didn't exist (false).
    /// </returns>
    public async Task<List<bool>> SafeDeleteTupleAsync(
        List<FgaQueueDeletePayload> tuples,
        CancellationToken ct = new()
    ) {
        return await SafeTupleOperationAsync(
            tuples,
            tuple => (tuple.RawUser, tuple.RawRelation, tuple.RawObject),
            exists => exists,
            tuplesToProcess => tuplesToProcess.Select(tuple => new ClientTupleKeyWithoutCondition {
                    User = tuple.RawUser,
                    Relation = tuple.RawRelation,
                    Object = tuple.RawObject,
                }
            ).ToList(),
            processedTuples => new ClientWriteRequest { Deletes = processedTuples },
            ct
        );
    }

    /// <summary>
    ///     Core method that handles safe tuple operations (write/delete) by checking existence first.
    /// </summary>
    /// <typeparam name="TPayload">The type of the payload (write or delete)</typeparam>
    /// <typeparam name="TClientTuple">The type of the client tuple (with or without condition)</typeparam>
    /// <param name="tuples">The list of tuples to process</param>
    /// <param name="extractCheckData">Function to extract check data from payload</param>
    /// <param name="shouldProcess">Function to determine if tuple should be processed based on existence</param>
    /// <param name="createClientTuples">Function to create client tuples from payloads</param>
    /// <param name="createWriteRequest">Function to create the write request</param>
    /// <param name="ct">The cancellation token</param>
    /// <returns>A list of booleans indicating which tuples were processed</returns>
    private async Task<List<bool>> SafeTupleOperationAsync<TPayload, TClientTuple>(
        List<TPayload> tuples,
        Func<TPayload, (string User, string Relation, string Object)> extractCheckData,
        Func<bool, bool> shouldProcess,
        Func<IEnumerable<TPayload>, List<TClientTuple>> createClientTuples,
        Func<List<TClientTuple>, ClientWriteRequest> createWriteRequest,
        CancellationToken ct
    ) {
        var results = new List<bool>();
        var tuplesToProcess = new List<TPayload>();

        // Check whether the tuples already exist
        var checks = tuples.Select(extractCheckData);
        var checkResults = await BatchCheckAsync(checks, ct);

        var checkResultsList = checkResults.ToList();
        for (int i = 0; i < tuples.Count; i++) {
            var tuple = tuples[i];
            var exists = checkResultsList[i];

            if (shouldProcess(exists)) {
                results.Add(true);
                tuplesToProcess.Add(tuple);
            } else {
                results.Add(false);
            }
        }

        if (tuplesToProcess.Count == 0) {
            return results;
        }

        var clientTuples = createClientTuples(tuplesToProcess);
        var writeRequest = createWriteRequest(clientTuples);

        await openFgaClient.Write(writeRequest, cancellationToken: ct);

        return results;
    }

    #endregion Write/Delete Methods
}
