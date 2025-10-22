using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Rules;
internal static class ReturnTaskFromResultInsteadOfReturningNullAnalyzerCommon
{
    internal static IMethodSymbol? FindContainingMethod(IOperation operation, CancellationToken cancellationToken)
    {
        return FindContainingMethod(operation.SemanticModel, operation.Syntax, cancellationToken);
    }

    internal static IMethodSymbol? FindContainingMethod(SemanticModel? semanticModel, SyntaxNode? syntaxNode, CancellationToken cancellationToken)
    {
        if (semanticModel is null)
            return null;

        while (syntaxNode is not null)
        {
            if (syntaxNode.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                var node = (AnonymousMethodExpressionSyntax)syntaxNode;
                if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is IMethodSymbol methodSymbol)
                    return methodSymbol;

                return null;
            }
            else if (syntaxNode.IsKind(SyntaxKind.ParenthesizedLambdaExpression) || syntaxNode.IsKind(SyntaxKind.SimpleLambdaExpression))
            {
                var node = (LambdaExpressionSyntax)syntaxNode;
                if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is IMethodSymbol methodSymbol)
                    return methodSymbol;

                return null;
            }
            else if (syntaxNode.IsKind(SyntaxKind.LocalFunctionStatement))
            {
                var node = (LocalFunctionStatementSyntax)syntaxNode;
                if (semanticModel.GetDeclaredSymbol(node, cancellationToken) is IMethodSymbol methodSymbol)
                    return methodSymbol;

                return null;
            }
            else if (syntaxNode.IsKind(SyntaxKind.MethodDeclaration))
            {
                var node = (MethodDeclarationSyntax)syntaxNode;
                if (semanticModel.GetDeclaredSymbol(node, cancellationToken) is IMethodSymbol methodSymbol)
                    return methodSymbol;

                return null;
            }

            syntaxNode = syntaxNode.Parent;
        }

        return null;
    }
}
