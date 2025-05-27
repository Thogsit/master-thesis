using System.Collections.Generic;

namespace SealedFga.Models;

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