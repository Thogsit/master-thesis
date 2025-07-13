using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenFga.Sdk.Model;
using SealedFga.AuthModel;
using SealedFga.Models;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Models;

namespace SealedFga.Fga;

/// <summary>
///     A service class that handles jobs related to FGA operations, such as writing and
///     deleting tuples in the OpenFGA store. This service is integrated with TickerQ functions
///     for task execution.
/// </summary>
public class SealedFgaQueue(SealedFgaService sealedFgaService) {
    public const string FgaWriteJobName = "FgaWriteJob";
    public const string FgaDeleteJobName = "FgaDeleteJob";
    public const string FgaWriteMultipleJobName = "FgaWriteMultipleJob";
    public const string FgaDeleteMultipleJobName = "FgaDeleteMultipleJob";
    public const string FgaWriteAndDeleteMultipleJobName = "FgaWriteAndDeleteMultipleJob";
    public const string FgaModifyIdJobName = "FgaModifyIdJob";

    /// <summary>
    ///     Handles the FGA write job by processing the payload and writing a tuple key to the FGA client.
    /// </summary>
    /// <param name="tickerContext">The context containing the request payload.</param>
    /// <param name="ct">The cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation of writing to the FGA client.</returns>
    [TickerFunction(FgaWriteJobName)]
    public async Task FgaWriteJob(TickerFunctionContext<TupleKey> tickerContext, CancellationToken ct)
        => await sealedFgaService.SafeWriteTupleAsync([tickerContext.Request], ct);

    /// <summary>
    ///     Handles the FGA delete job by processing the payload and deleting a tuple key in the FGA client.
    /// </summary>
    /// <param name="tickerContext">The context containing the request payload.</param>
    /// <param name="ct">The cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation of deleting in the FGA client.</returns>
    [TickerFunction(FgaDeleteJobName)]
    public async Task FgaDeleteJob(TickerFunctionContext<TupleKey> tickerContext, CancellationToken ct)
        => await sealedFgaService.SafeDeleteTupleAsync([tickerContext.Request], ct);

    /// <summary>
    ///     Handles the FGA write multiple job by processing a collection of payloads and writing multiple tuple keys to the
    ///     FGA client.
    /// </summary>
    /// <param name="tickerContext">
    ///     The context containing the collection of request payloads of type
    ///     <see cref="IEnumerable{FgaQueueWritePayload}" />.
    /// </param>
    /// <param name="ct">The cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation of writing multiple entries to the FGA client.</returns>
    [TickerFunction(FgaWriteMultipleJobName)]
    public async Task FgaWriteMultipleJob(
        TickerFunctionContext<IEnumerable<TupleKey>> tickerContext,
        CancellationToken ct
    ) => await sealedFgaService.SafeWriteTupleAsync(tickerContext.Request.ToList(), ct);

    /// <summary>
    ///     Handles the FGA delete multiple job by processing a collection of payloads and deleting multiple tuple keys from
    ///     the FGA client.
    /// </summary>
    /// <param name="tickerContext">
    ///     The context containing a collection of request payloads of type <see cref="IEnumerable{FgaQueueDeletePayload}" />.
    /// </param>
    /// <param name="ct">The cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation of deleting multiple tuple keys from the FGA client.</returns>
    [TickerFunction(FgaDeleteMultipleJobName)]
    public async Task FgaDeleteMultipleJob(
        TickerFunctionContext<IEnumerable<TupleKey>> tickerContext,
        CancellationToken ct
    ) => await sealedFgaService.SafeDeleteTupleAsync(tickerContext.Request.ToList(), ct);

    [TickerFunction(FgaWriteAndDeleteMultipleJobName)]
    public async Task FgaWriteAndDeleteMultipleJob(
        TickerFunctionContext<(IEnumerable<TupleKey> Writes, IEnumerable<TupleKey> Deletes)> tickerContext,
        CancellationToken ct
    ) => await sealedFgaService.SafeWriteAndDeleteTuplesAsync(
        tickerContext.Request.Writes.ToList(),
        tickerContext.Request.Deletes.ToList(),
        ct
    );

    /// <summary>
    ///     Handles the FGA ID modification job by updating entries in the FGA client
    ///     based on the old and new IDs provided in the payload.
    /// </summary>
    /// <param name="tickerContext">
    ///     The context containing the payload with the old ID and the new ID to be updated.
    /// </param>
    /// <param name="ct">The cancellation token to cancel the operation if needed.</param>
    /// <typeparam name="TId">
    ///     The type of the ID being modified, which implements <see cref="ISealedFgaTypeId{TId}" />.
    /// </typeparam>
    /// <returns>
    ///     A task that represents the asynchronous operation of modifying IDs in the FGA client.
    /// </returns>
    [TickerFunction(FgaModifyIdJobName)]
    public async Task FgaModifyIdJob(
        TickerFunctionContext<FgaQueueModifyIdPayload> tickerContext,
        CancellationToken ct
    ) {
        var payload = tickerContext.Request;
        await sealedFgaService.ModifyIdAsync(payload.RawOldId, payload.RawNewId, payload.TypeName, ct);
    }
}
