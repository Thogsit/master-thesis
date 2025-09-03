using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Model;
using SealedFga.AuthModel;
using SealedFga.Util;
using TickerQ.Utilities;
using TickerQ.Utilities.Interfaces.Managers;
using TickerQ.Utilities.Models.Ticker;
using Tuple = OpenFga.Sdk.Model.Tuple;

namespace SealedFga.Fga;

/// <summary>
///     Wrapper class for communicating with the OpenFGA service using strongly typed IDs.
///     Handles reads directly; queues writes/deletes for reliable processing.
/// </summary>
public class SealedFgaService(
    OpenFgaClient openFgaClient,
    ITimeTickerManager<TimeTicker> tickerQ,
    IOptions<SealedFgaOptions> options
) {
    private readonly SealedFgaOptions _options = options.Value;

    private static readonly int[] DefaultRetryIntervals = [
        TimeSpan.FromMinutes(1).Seconds,
        TimeSpan.FromMinutes(10).Seconds,
        TimeSpan.FromHours(1).Seconds,
    ];

    private static readonly int DefaultRetryCount = DefaultRetryIntervals.Length;

    private TimeTicker CreateTimeTicker<TReq>(
        string functionName,
        TReq request,
        int? retries = null,
        int[]? retryIntervals = null
    ) {
        retries ??= DefaultRetryCount;
        retryIntervals ??= DefaultRetryIntervals;
        return new TimeTicker {
            Function = functionName,
            Request = TickerHelper.CreateTickerRequest(request),
            Retries = retries.Value,
            ExecutionTime = DateTime.Now.AddSeconds(1),
            RetryIntervals = retryIntervals,
        };
    }

    #region Strongly-Typed ID Methods

    /// <summary>
    ///     Modifies all tuples containing a reference to the old ID and modifies them to reference the new ID.
    /// </summary>
    /// <param name="oldId">The current identifier to be updated.</param>
    /// <param name="newId">The new identifier to replace the old one.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <typeparam name="TId">The type of the identifier, which implements <see cref="ISealedFgaTypeId{TId}" />.</typeparam>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ModifyIdAsync<TId>(
        TId oldId,
        TId newId,
        CancellationToken cancellationToken = new()
    ) where TId : ISealedFgaTypeId<TId>
        => await ModifyIdAsync(
            oldId.AsOpenFgaIdTupleString(),
            newId.AsOpenFgaIdTupleString(),
            IdUtil.GetNameByIdType(typeof(TId)),
            cancellationToken
        );


    /// <summary>
    ///     Ensures authorization using strongly typed IDs, throwing an exception if the check fails.
    /// </summary>
    /// <param name="user">The user ID (strongly typed)</param>
    /// <param name="relation">The relation string</param>
    /// <param name="objectId">The object ID (strongly typed)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TUserId">The user ID type</typeparam>
    /// <typeparam name="TObjId">The object ID type</typeparam>
    /// <exception cref="UnauthorizedAccessException">Thrown when the authorization check fails</exception>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task EnsureCheckAsync<TUserId, TObjId>(
        TUserId user,
        ISealedFgaRelation<TObjId> relation,
        TObjId objectId,
        CancellationToken cancellationToken = new()
    )
        where TUserId : ISealedFgaTypeId<TUserId>
        where TObjId : ISealedFgaTypeId<TObjId> {
        if (await CheckAsync(user, relation, objectId, cancellationToken)) {
            throw new UnauthorizedAccessException(
                $"Access denied: User '{user}' does not have relation '{relation}' to object '{objectId}'"
            );
        }
    }

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
        ISealedFgaRelation<TObjId> relation,
        TObjId objectId,
        CancellationToken cancellationToken = new()
    )
        where TUserId : ISealedFgaTypeId<TUserId>
        where TObjId : ISealedFgaTypeId<TObjId>
        => await CheckAsync(new TupleKey {
                User = user.AsOpenFgaIdTupleString(),
                Relation = relation.AsOpenFgaString(),
                Object = objectId.AsOpenFgaIdTupleString(),
            },
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
        ISealedFgaRelation<TObjId> relation,
        TObjId objectId
    )
        where TUserId : ISealedFgaTypeId<TUserId>
        where TObjId : ISealedFgaTypeId<TObjId>
        => await QueueWrite(new TupleKey {
                User = user.AsOpenFgaIdTupleString(),
                Relation = relation.AsOpenFgaString(),
                Object = objectId.AsOpenFgaIdTupleString(),
            }
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
        ISealedFgaRelation<TObjId> relation,
        TObjId objectId
    )
        where TUserId : ISealedFgaTypeId<TUserId>
        where TObjId : ISealedFgaTypeId<TObjId>
        => await QueueDelete(new TupleKey {
                User = user.AsOpenFgaIdTupleString(),
                Relation = relation.AsOpenFgaString(),
                Object = objectId.AsOpenFgaIdTupleString(),
            }
        );

    /// <summary>
    ///     Queues multiple write operations using strongly typed IDs.
    /// </summary>
    /// <param name="operations">Collection of write operations with strongly typed IDs</param>
    /// <typeparam name="TUserId">The user ID type</typeparam>
    /// <typeparam name="TObjId">The object ID type</typeparam>
    public async Task QueueWrites<TUserId, TObjId>(
        IEnumerable<(TUserId User, ISealedFgaRelation<TObjId> Relation, TObjId Object)> operations
    )
        where TUserId : ISealedFgaTypeId<TUserId>
        where TObjId : ISealedFgaTypeId<TObjId>
        => await QueueWrites(
            operations.Select(op => new TupleKey {
                    User = op.User.AsOpenFgaIdTupleString(),
                    Relation = op.Relation.AsOpenFgaString(),
                    Object = op.Object.AsOpenFgaIdTupleString(),
                }
            )
        );

    /// <summary>
    ///     Queues multiple delete operations using strongly typed IDs.
    /// </summary>
    /// <param name="operations">Collection of delete operations with strongly typed IDs</param>
    /// <typeparam name="TUserId">The user ID type</typeparam>
    /// <typeparam name="TObjId">The object ID type</typeparam>
    public async Task QueueDeletes<TUserId, TObjId>(
        IEnumerable<(TUserId User, ISealedFgaRelation<TObjId> Relation, TObjId Object)> operations
    )
        where TUserId : ISealedFgaTypeId<TUserId>
        where TObjId : ISealedFgaTypeId<TObjId>
        => await QueueDeletes(
            operations.Select(op => new TupleKey {
                    User = op.User.AsOpenFgaIdTupleString(),
                    Relation = op.Relation.AsOpenFgaString(),
                    Object = op.Object.AsOpenFgaIdTupleString(),
                }
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
        ISealedFgaRelation<TObjId> relation,
        CancellationToken cancellationToken = new()
    )
        where TUserId : ISealedFgaTypeId<TUserId>
        where TObjId : ISealedFgaTypeId<TObjId> {
        var objectStrings = await ListObjectsAsync(
            user.AsOpenFgaIdTupleString(),
            relation.AsOpenFgaString(),
            IdUtil.GetNameByIdType(typeof(TObjId)),
            cancellationToken
        );

        return objectStrings.Select(IdUtil.ParseId<TObjId>);
    }

    /// <summary>
    ///     Performs batch check operations using strongly typed IDs.
    /// </summary>
    /// <param name="checks">List of check requests with strongly typed IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TUserId">The user ID type</typeparam>
    /// <typeparam name="TObjId">The object ID type</typeparam>
    /// <returns>Dictionary with results for each check</returns>
    public async Task<Dictionary<(TUserId User, ISealedFgaRelation<TObjId> Relation, TObjId Object), bool>>
        BatchCheckAsync<TUserId, TObjId>(
            IEnumerable<(TUserId User, ISealedFgaRelation<TObjId> Relation, TObjId Object)> checks,
            CancellationToken cancellationToken = new()
        )
        where TUserId : ISealedFgaTypeId<TUserId>
        where TObjId : ISealedFgaTypeId<TObjId> {
        var checksAsList = checks.ToList();
        var results = await BatchCheckAsync(
            checksAsList.Select(check => new TupleKey {
                    User = check.User.AsOpenFgaIdTupleString(),
                    Relation = check.Relation.AsOpenFgaString(),
                    Object = check.Object.AsOpenFgaIdTupleString(),
                }
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
    /// <param name="tuple">The tuple to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the relation exists, false otherwise</returns>
    internal async Task<bool> CheckAsync(
        TupleKey tuple,
        CancellationToken cancellationToken = new()
    ) {
        var response = await openFgaClient.Check(new ClientCheckRequest {
                User = tuple.User,
                Relation = tuple.Relation,
                Object = tuple.Object,
            },
            cancellationToken: cancellationToken
        );

        return response.Allowed ?? false;
    }

    /// <summary>
    ///     Modifies all relations that include a reference to the specified old raw ID and updates them to reference the new
    ///     raw ID.
    /// </summary>
    /// <param name="rawOldId">The current raw identifier to be updated.</param>
    /// <param name="rawNewId">The new raw identifier to replace the old one.</param>
    /// <param name="typeName">The name of the type associated with the ID for contextual identification.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ModifyIdAsync(
        string rawOldId,
        string rawNewId,
        string typeName,
        CancellationToken cancellationToken = new()
    ) {
        // Find relations TO the old ID
        var oldRelationTuples = await ListAllRelationsToObjectAsync(
            rawOldId,
            cancellationToken
        );

        // Find relations FROM the old ID
        oldRelationTuples.AddRange(
            await ListAllRelationsFromUserAsync(
                rawOldId,
                typeName,
                cancellationToken
            )
        );

        // Build new relations to replace old ones with
        var newRelationTuples = oldRelationTuples.Select(tuple => new TupleKey {
                User = tuple.User.Replace(rawOldId, rawNewId),
                Relation = tuple.Relation,
                Object = tuple.Object.Replace(rawOldId, rawNewId),
            }
        ).ToList();

        // Update relations to OpenFGA
        await openFgaClient.Write(new ClientWriteRequest {
                Deletes = oldRelationTuples.Select(tuple => new ClientTupleKeyWithoutCondition {
                        User = tuple.User,
                        Relation = tuple.Relation,
                        Object = tuple.Object,
                    }
                ).ToList(),
                Writes = newRelationTuples.Select(tuple => new ClientTupleKey {
                        User = tuple.User,
                        Relation = tuple.Relation,
                        Object = tuple.Object,
                    }
                ).ToList(),
            },
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    ///     Queues a write operation using raw strings.
    /// </summary>
    /// <param name="tuple">The tuple to write</param>
    internal async Task QueueWrite(TupleKey tuple) {
        if (_options.QueueFgaServiceOperations) {
            await tickerQ.AddAsync(
                CreateTimeTicker(
                    SealedFgaQueue.FgaWriteJobName,
                    tuple
                )
            );
        } else {
            _ = Task.Run(() => SafeWriteTupleAsync([tuple]));
        }
    }

    /// <summary>
    ///     Queues a delete operation using raw strings.
    /// </summary>
    /// <param name="tuple">The tuple to delete</param>
    internal async Task QueueDelete(TupleKey tuple) {
        if (_options.QueueFgaServiceOperations) {
            await tickerQ.AddAsync(
                CreateTimeTicker(
                    SealedFgaQueue.FgaDeleteJobName,
                    tuple
                )
            );
        } else {
            _ = Task.Run(() => SafeDeleteTupleAsync([tuple]));
        }
    }

    /// <summary>
    ///     Queues multiple write operations using raw strings.
    /// </summary>
    /// <param name="tuples">Collection of tuples to write</param>
    internal async Task QueueWrites(IEnumerable<TupleKey> tuples) {
        if (_options.QueueFgaServiceOperations) {
            await tickerQ.AddAsync(
                CreateTimeTicker(
                    SealedFgaQueue.FgaWriteMultipleJobName,
                    tuples
                )
            );
        } else {
            _ = Task.Run(() => SafeWriteTupleAsync(tuples.ToList()));
        }
    }

    /// <summary>
    ///     Queues multiple delete operations using raw strings.
    /// </summary>
    /// <param name="tuples">Collection of tuples to delete</param>
    internal async Task QueueDeletes(IEnumerable<TupleKey> tuples) {
        if (_options.QueueFgaServiceOperations) {
            await tickerQ.AddAsync(
                CreateTimeTicker(
                    SealedFgaQueue.FgaDeleteMultipleJobName,
                    tuples
                )
            );
        } else {
            _ = Task.Run(() => SafeDeleteTupleAsync(tuples.ToList()));
        }
    }

    /// <summary>
    ///     Queues a modify ID operation to replace all relations with an old ID with a new ID.
    /// </summary>
    /// <param name="oldId">The previous ID to be replaced</param>
    /// <param name="newId">The new ID to replace the old one</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <typeparam name="TId">The type of the IDs</typeparam>
    /// <returns>A task that represents the asynchronous operation</returns>
    internal async Task QueueModifyId<TId>(
        TId oldId,
        TId newId,
        CancellationToken cancellationToken = new()
    ) where TId : ISealedFgaTypeId<TId> {
        if (_options.QueueFgaServiceOperations) {
            await tickerQ.AddAsync(
                CreateTimeTicker(
                    SealedFgaQueue.FgaModifyIdJobName,
                    (oldId, newId)
                ),
                cancellationToken
            );
        } else {
            _ = Task.Run(() => ModifyIdAsync(oldId, newId, cancellationToken), cancellationToken);
        }
    }

    /// <summary>
    ///     Queues multiple write and delete operations for processing.
    /// </summary>
    /// <param name="writes">The collection of tuple keys to be written.</param>
    /// <param name="deletes">The collection of tuple keys to be deleted.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal async Task QueueWriteAndDeletes(
        IEnumerable<TupleKey> writes,
        IEnumerable<TupleKey> deletes,
        CancellationToken cancellationToken = new()
    ) {
        if (_options.QueueFgaServiceOperations) {
            await tickerQ.AddAsync(
                CreateTimeTicker(
                    SealedFgaQueue.FgaWriteAndDeleteMultipleJobName,
                    (writes, deletes)
                ),
                cancellationToken
            );
        } else {
            _ = Task.Run(() => SafeWriteAndDeleteTuplesAsync(writes.ToList(), deletes.ToList(), cancellationToken), cancellationToken);
        }
    }

    /// <summary>
    ///     Lists objects that a user has a specific relation to using raw strings.
    /// </summary>
    /// <param name="rawUser">The user string</param>
    /// <param name="rawRelation">The relation string</param>
    /// <param name="objectTypeName">The type of objects to list</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of object strings</returns>
    internal async Task<IEnumerable<string>> ListObjectsAsync(
        string rawUser,
        string rawRelation,
        string objectTypeName,
        CancellationToken cancellationToken = new()
    ) {
        var response = await openFgaClient.ListObjects(new ClientListObjectsRequest {
                User = rawUser,
                Relation = rawRelation,
                Type = objectTypeName,
            },
            cancellationToken: cancellationToken
        );

        return response.Objects;
    }

    /// <summary>
    ///     Retrieves all relations associated with a specific object.
    /// </summary>
    /// <param name="rawObject">The ID of the object for which relations are to be listed.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>
    ///     An asynchronous task containing a collection of tuples representing relations tied to the specified object.
    /// </returns>
    internal async Task<List<TupleKey>> ListAllRelationsToObjectAsync(
        string rawObject,
        CancellationToken cancellationToken = new()
    ) {
        var readRequest = new ClientReadRequest {
            Object = rawObject,
        };

        var response = await openFgaClient.Read(readRequest, cancellationToken: cancellationToken);
        return response.Tuples.Select(t => t.Key).ToList();
    }

    /// <summary>
    ///     Retrieves all relation tuples where the specified user is related to other objects within the FGA system.
    /// </summary>
    /// <param name="rawUser">The ID of the user for whom relations are to be retrieved</param>
    /// <param name="userTypeName">The type name of the user</param>
    /// <param name="cancellationToken">Cancellation token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of relation tuples</returns>
    internal async Task<List<TupleKey>> ListAllRelationsFromUserAsync(
        string rawUser,
        string userTypeName,
        CancellationToken cancellationToken = new()
    ) {
        // Retrieve possible relations the object can have as a subject to other objects
        var authModel = await openFgaClient.ReadAuthorizationModel(cancellationToken: cancellationToken);
        var typeDefinitions = authModel.AuthorizationModel!.TypeDefinitions;
        var listObjectsRequests = new List<ClientListObjectsRequest>();
        foreach (var typeDef in typeDefinitions) {
            foreach (var relationDef in typeDef.Metadata!.Relations!) {
                var directlyRelatedUserTypes = relationDef.Value.DirectlyRelatedUserTypes!;
                foreach (var relationRef in directlyRelatedUserTypes) {
                    if (relationRef.Type == userTypeName) {
                        listObjectsRequests.Add(
                            new ClientListObjectsRequest {
                                User = rawUser,
                                Relation = relationDef.Key,
                                Type = typeDef.Type,
                            }
                        );
                    }
                }
            }
        }

        // Retrieve all objects we're related to and add the relations to our deletion list
        var relationTuples = new List<TupleKey>();
        foreach (var listObjectsBody in listObjectsRequests) {
            var listObjectsResponse = await openFgaClient.ListObjects(
                listObjectsBody,
                cancellationToken: cancellationToken
            );
            relationTuples.AddRange(
                listObjectsResponse.Objects.Select(obj => new TupleKey {
                        User = rawUser,
                        Relation = listObjectsBody.Relation,
                        Object = obj,
                    }
                )
            );
        }

        return relationTuples;
    }

    /// <summary>
    ///     Performs batch check operations using raw strings.
    /// </summary>
    /// <param name="checks">List of check requests with raw strings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Results in the same order as input checks</returns>
    internal async Task<IEnumerable<bool>> BatchCheckAsync(
        IEnumerable<TupleKey> checks,
        CancellationToken cancellationToken = new()
    ) {
        // TODO: OpenFGA .NET SDK does not support batch check operations directly. Switch to them when available.
        var checkTasks = checks.Select(async check => {
                try {
                    return await CheckAsync(check, cancellationToken);
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
    ///     A task that represents the asynchronous operation.
    /// </returns>
    public async Task SafeWriteTupleAsync(
        List<TupleKey> tuples,
        CancellationToken ct = new()
    ) => await SafeWriteAndDeleteTuplesAsync( // No delete tuples for write operation
        tuples,
        [],
        ct
    );

    /// <summary>
    ///     Safely deletes a list of tuples from OpenFGA after checking if they exist.
    ///     This prevents failures when attempting to delete tuples that don't exist.
    /// </summary>
    /// <param name="tuples">The list of tuples to delete</param>
    /// <param name="ct">The cancellation token to cancel the operation if needed</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SafeDeleteTupleAsync(List<TupleKey> tuples, CancellationToken ct = new())
        => await SafeWriteAndDeleteTuplesAsync([], // No write tuples for delete operation
            tuples,
            ct
        );

    /// <summary>
    ///     Executes a safe operation for tuple deletion and writing in batches, ensuring that
    ///     only necessary tuples are processed based on the results of batch checks.
    /// </summary>
    /// <param name="writeTuples">A list of tuples to be checked and potentially written.</param>
    /// <param name="deleteTuples">A list of tuples to be checked and potentially deleted.</param>
    /// <param name="ct">An optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SafeWriteAndDeleteTuplesAsync(
        List<TupleKey> writeTuples,
        List<TupleKey> deleteTuples,
        CancellationToken ct = new()
    ) {
        // Check all tuples to avoid conflicts
        var deleteChecks = await BatchCheckAsync(
            deleteTuples,
            ct
        );
        var writeChecks = await BatchCheckAsync(
            writeTuples,
            ct
        );

        // Only process tuples that need to be deleted or written
        var deleteRequests = deleteTuples
                            .Where((_, index) => deleteChecks.ElementAt(index))
                            .Select(tuple => new ClientTupleKeyWithoutCondition {
                                     User = tuple.User,
                                     Relation = tuple.Relation,
                                     Object = tuple.Object,
                                 }
                             )
                            .ToList();
        var writeRequests = writeTuples
                           .Where((_, index) => !writeChecks.ElementAt(index))
                           .Select(tuple => new ClientTupleKey {
                                    User = tuple.User,
                                    Relation = tuple.Relation,
                                    Object = tuple.Object,
                                }
                            )
                           .ToList();

        // Execute requests
        if (deleteRequests.Count > 0 || writeRequests.Count > 0) {
            var writeRequest = new ClientWriteRequest {
                Deletes = deleteRequests,
                Writes = writeRequests,
            };

            await openFgaClient.Write(writeRequest, cancellationToken: ct);
        }
    }

    #endregion Write/Delete Methods
}
