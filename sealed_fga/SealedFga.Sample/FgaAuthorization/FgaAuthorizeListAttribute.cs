using System;
using Microsoft.AspNetCore.Mvc;

namespace SealedFga.Sample.FgaAuthorization;

[AttributeUsage(AttributeTargets.Parameter)]
public class FgaAuthorizeListAttribute() : ModelBinderAttribute(typeof(SealedFgaEntityListModelBinder))
{
    public required string Relation { get; set; }
}