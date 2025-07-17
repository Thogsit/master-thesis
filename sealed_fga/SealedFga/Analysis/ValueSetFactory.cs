using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;

namespace SealedFga.Analysis;

/// <summary>
///     Factory class for creating GlobalFlowStateAnalysisValueSet instances from SealedFgaDataFlowValue instances.
///     This bridges the gap between our AnalysisEntity-based authorization analysis and Microsoft's
///     GlobalFlowStateAnalysis framework.
/// </summary>
internal static class ValueSetFactory {
    /// <summary>
    ///     Creates a GlobalFlowStateAnalysisValueSet from a SealedFgaDataFlowValue.
    /// </summary>
    /// <param name="dataFlowValue">The SealedFgaDataFlowValue to wrap</param>
    /// <returns>A GlobalFlowStateAnalysisValueSet containing the authorization state</returns>
    public static GlobalFlowStateAnalysisValueSet Create(SealedFgaDataFlowValue dataFlowValue) =>
        GlobalFlowStateAnalysisValueSet.Create(dataFlowValue);

    /// <summary>
    ///     Creates a GlobalFlowStateAnalysisValueSet with an empty authorization state.
    /// </summary>
    /// <returns>A GlobalFlowStateAnalysisValueSet with bottom authorization state</returns>
    public static GlobalFlowStateAnalysisValueSet CreateEmpty() => Create(new SealedFgaDataFlowValue());
}
