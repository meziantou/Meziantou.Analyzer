using Microsoft.CodeAnalysis.Simplification;

namespace Meziantou.Analyzer.Internals;
internal static class ParenthesesExtensions
{
    public static SyntaxNode Parentheses(this SyntaxNode syntaxNode)
    {
        if (syntaxNode is ExpressionSyntax expression)
        {
            return SyntaxFactory.ParenthesizedExpression(expression).WithAdditionalAnnotations(Simplifier.Annotation);
        }

        return syntaxNode;
    }

    public static ParenthesizedExpressionSyntax Parentheses(this ExpressionSyntax expression)
    {
        return SyntaxFactory.ParenthesizedExpression(expression).WithAdditionalAnnotations(Simplifier.Annotation);
    }
}
