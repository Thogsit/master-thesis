using System;
using Microsoft.AspNetCore.Mvc;
using SealedFga.ModelBinder;

namespace SealedFga.Attributes;

/// <summary>
///     Specifies that the parameter should be bound as a list using FGA authorization for a specific relation.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FgaAuthorizeListAttribute() : ModelBinderAttribute(typeof(SealedFgaEntityListModelBinder)) {
    /// <summary>
    ///     The relation to check for authorization.
    /// </summary>
    public required string Relation { get; set; }
}
