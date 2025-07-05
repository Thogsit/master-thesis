using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SealedFga.Models.JobPayloads;
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

    #region Job Handlers

    /// <summary>
    ///     Handles the FGA write job by processing the payload and writing a tuple key to the FGA client.
    /// </summary>
    /// <param name="tickerContext">The context containing the request payload of type <see cref="FgaQueueWritePayload" />.</param>
    /// <param name="ct">The cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation of writing to the FGA client.</returns>
    [TickerFunction(FgaWriteJobName)]
    public async Task FgaWriteJob(TickerFunctionContext<FgaQueueWritePayload> tickerContext, CancellationToken ct)
        => await sealedFgaService.SafeWriteTupleAsync([tickerContext.Request], ct);

    /// <summary>
    ///     Handles the FGA delete job by processing the payload and deleting a tuple key in the FGA client.
    /// </summary>
    /// <param name="tickerContext">The context containing the request payload of type <see cref="FgaQueueDeletePayload" />.</param>
    /// <param name="ct">The cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation of deleting in the FGA client.</returns>
    [TickerFunction(FgaDeleteJobName)]
    public async Task FgaDeleteJob(TickerFunctionContext<FgaQueueDeletePayload> tickerContext, CancellationToken ct)
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
        TickerFunctionContext<IEnumerable<FgaQueueWritePayload>> tickerContext,
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
        TickerFunctionContext<IEnumerable<FgaQueueDeletePayload>> tickerContext,
        CancellationToken ct
    ) => await sealedFgaService.SafeDeleteTupleAsync(tickerContext.Request.ToList(), ct);

    #endregion Job Handlers
}
