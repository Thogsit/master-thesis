using Microsoft.CodeAnalysis;

namespace SealedFga.Analysis;

public class HttpEndpointAnalysisContext(
    IMethodSymbol methodSymbol,
    SyntaxNode methodSyntax,
    SemanticModel methodSemanticModel
)
{
    public IMethodSymbol MethodSymbol { get; set; } = methodSymbol;
    public SyntaxNode MethodSyntax { get; set; } = methodSyntax;
    public SemanticModel MethodSemanticModel { get; set; } = methodSemanticModel;
}