using Microsoft.CodeAnalysis;
using OpenFga.Language.Model;

namespace SealedFga.Models;

public record struct ModelFgaIncrementalChange
{
    public Location? DiagnosticLocation { get; set; }
    public AuthorizationModel? AuthorizationModel { get; set; }
}