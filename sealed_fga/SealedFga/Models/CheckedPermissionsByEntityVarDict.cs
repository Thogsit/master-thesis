using System.Collections.Generic;

namespace SealedFga.Models;

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
public class CheckedPermissionsByEntityVarDict : Dictionary<string, HashSet<string>>
{
    public void AddPermission(string entityVar, string permission)
    {
        if (!TryGetValue(entityVar, out var permissions))
        {
            permissions = [];
            Add(entityVar, permissions);
        }

        permissions.Add(permission);
    }
}