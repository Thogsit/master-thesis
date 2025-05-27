using System.Collections.Generic;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;
using SealedFga.Models;

namespace SealedFga.Analysis;

internal class OpenFgaDataFlowVisitor(
    GlobalFlowStateAnalysisContext analysisContext,
    CheckedPermissionsByEntityVarDict checkedPermissionsByEntityId
) : GlobalFlowStateValueSetFlowOperationVisitor(analysisContext, hasPredicatedGlobalState: true)
{
    /// <summary>
    ///     Contains all so far checked permissions per entity ID
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///  "type:someId": [
    ///      "can_read",
    ///      "can_write"
    ///  ],
    ///  "type:otherId": [
    ///      "can_read"
    ///  ]
    /// }
    /// </code>
    /// </example>
    public CheckedPermissionsByEntityVarDict CheckedPermissionsByEntityId { get; } = checkedPermissionsByEntityId;
    
    
}