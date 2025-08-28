using Microsoft.CodeAnalysis;

namespace SealedFga.Analysis;

public class RequireCheckAnalysisContext(
    IMethodSymbol containingMethodSymbol,
    SyntaxNode requireCheckSyntax,
    SemanticModel semanticModel
) {
    public IMethodSymbol ContainingMethodSymbol { get; set; } = containingMethodSymbol;
    public SyntaxNode RequireCheckSyntax { get; set; } = requireCheckSyntax;
    public SemanticModel SemanticModel { get; set; } = semanticModel;
}
