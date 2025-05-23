using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;

namespace SealedFga.Analysis;

internal class OpenFgaDataFlowVisitor : GlobalFlowStateValueSetFlowOperationVisitor
{
    public OpenFgaDataFlowVisitor(GlobalFlowStateAnalysisContext analysisContext)
        : base(analysisContext, hasPredicatedGlobalState: true)
    {
    }
}