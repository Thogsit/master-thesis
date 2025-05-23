using System;
using Microsoft.AspNetCore.Mvc;

namespace SealedFga.Sample.FgaAuthorization;

[AttributeUsage(AttributeTargets.Parameter)]
public class FgaAuthorizeAttribute() : ModelBinderAttribute(typeof(SealedFgaEntityModelBinder))
{
    public required string Relation { get; set; }
    public string ParameterName { get; set; }
}