using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis;

namespace SealedFga.Analysis;

internal class SealedFgaDataFlowValue(
    Dictionary<string, HashSet<string>> checkedPermissionsByEntityId,
    bool negated
) : IAbstractAnalysisValue, IEquatable<SealedFgaDataFlowValue> {
    /// <summary>
    ///     Contains all so far checked permissions per entity ID
    /// </summary>
    /// <example>
    ///     <code>
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
    public Dictionary<string, HashSet<string>> CheckedPermissionsByEntityId { get; } = checkedPermissionsByEntityId;

    public bool Negated { get; } = negated;

    public IAbstractAnalysisValue GetNegatedValue()
        => new SealedFgaDataFlowValue(CheckedPermissionsByEntityId, !Negated);

    public bool Equals(IAbstractAnalysisValue other)
        => other is SealedFgaDataFlowValue otherValue && Equals(otherValue);

    public bool Equals(SealedFgaDataFlowValue other) {
        var otherCheckedPermissionsByEntityId = other.CheckedPermissionsByEntityId;

        // Reference equals
        if (CheckedPermissionsByEntityId == otherCheckedPermissionsByEntityId) return true;

        // Content equals
        if (CheckedPermissionsByEntityId.Count != otherCheckedPermissionsByEntityId.Count) return false;
        foreach (var kvp in CheckedPermissionsByEntityId) {
            if (!otherCheckedPermissionsByEntityId.TryGetValue(kvp.Key, out var otherCheckedPermissions)) return false;
            if (!kvp.Value.SetEquals(otherCheckedPermissions)) return false;
        }

        return true;
    }
}
