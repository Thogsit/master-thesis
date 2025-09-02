using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SealedFga.Util;

namespace SealedFga.Analysis;

public class DepInjectionSyntaxRewriter(
    Dictionary<ITypeSymbol, INamedTypeSymbol> interfaceRedirects,
    SourceLocationMapper locationMapper,
    SemanticModel semanticModel
) : CSharpSyntaxRewriter {
    public override SyntaxNode? VisitParameter(ParameterSyntax node) {
        // Only analyze primary class constructor parameters for now
        // TODO: We will want to support normal constructors and possibly other kinds of parameters in the future!
        if (node.Type == null || node.Parent?.Parent is not ClassDeclarationSyntax) {
            return base.VisitParameter(node);
        }

        var typeSymbol = semanticModel.GetTypeInfo(node.Type).Type;
        if (typeSymbol != null && interfaceRedirects.TryGetValue(typeSymbol, out var redirectType)) {
            var originalType = node.Type;
            var newType = BuildIdentifierName(originalType, redirectType);

            locationMapper.AddMapping(originalType.SpanStart, originalType.Span.Length, newType.Span.Length);

            return node.WithType(newType);
        }

        return base.VisitParameter(node);
    }

    private static NameSyntax BuildIdentifierName(TypeSyntax originalType, INamedTypeSymbol redirectType) {
        var qualifiedName = redirectType.ContainingNamespace.FullName() + "." + redirectType.Name;
        var currentNameSyntax = SyntaxFactory.ParseName(qualifiedName);

        return currentNameSyntax
              .WithLeadingTrivia(originalType.GetLeadingTrivia())
              .WithTrailingTrivia(originalType.GetTrailingTrivia());
    }
}
