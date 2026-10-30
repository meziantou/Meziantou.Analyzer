namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class FloatingPointToDecimalConversionFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseDecimalLiteralInsteadOfConvertingFloatingPointConstant);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (root is null || nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (FindConvertedFloatingPointValue(semanticModel, nodeToFix, context.CancellationToken) is not { } value)
            return;

        if (CreateDecimalLiteral(value.Syntax) is not { } literal)
            return;

        // Replacing a conversion to decimal? by a decimal literal would change the type of the expression
        var nodeToReplace = semanticModel.GetTypeInfo(nodeToFix, context.CancellationToken).Type is { SpecialType: SpecialType.System_Decimal } ? nodeToFix : value.Syntax;
        var newNode = FixPrecedenceAndSpacing(nodeToReplace, literal).WithTriviaFrom(nodeToReplace);

        var title = "Use a decimal literal";
        context.RegisterCodeFix(
            CodeAction.Create(title, _ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(nodeToReplace, newNode))), equivalenceKey: title),
            context.Diagnostics);
    }

    private static IOperation? FindConvertedFloatingPointValue(SemanticModel semanticModel, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        // (decimal?)2.3 is several nested conversions bound to the cast, and Roslyn 5.0 and earlier versions do not return an operation for the cast
        foreach (var node in nodeToFix.AncestorsAndSelf())
        {
            if (semanticModel.GetOperation(node, cancellationToken) is not { } operation)
                continue;

            return operation.DescendantsAndSelf()
                .Where(operation => operation.Syntax == nodeToFix)
                .Select(FloatingPointToDecimalConversionCommon.GetConvertedFloatingPointValue)
                .FirstOrDefault(value => value is not null);
        }

        return null;
    }

    private static ExpressionSyntax? CreateDecimalLiteral(SyntaxNode syntax)
    {
        var expression = UnwrapParentheses(syntax);
        var isNegative = false;
        if (expression is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.UnaryMinusExpression } unaryExpression)
        {
            isNegative = true;
            expression = UnwrapParentheses(unaryExpression.Operand);
        }

        if (expression is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression } literal)
            return null;

        // Keep the text written by the developer (digit separators, exponent) and replace the type suffix
        var text = literal.Token.Text;
        if (literal.Token.Value is float or double && text[^1] is 'f' or 'F' or 'd' or 'D')
        {
            text = text[..^1];
        }

        text += "m";
        var token = SyntaxFactory.ParseToken(text);
        if (!token.IsKind(SyntaxKind.NumericLiteralToken) || token.Value is not decimal || token.ContainsDiagnostics || token.Span.Length != text.Length)
            return null;

        ExpressionSyntax result = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, token);
        if (isNegative)
        {
            result = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, result);
        }

        return result;
    }

    private static ExpressionSyntax FixPrecedenceAndSpacing(SyntaxNode nodeToReplace, ExpressionSyntax newNode)
    {
        if (newNode is not PrefixUnaryExpressionSyntax)
            return newNode;

        // new decimal(-1.5).ToString() => (-1.5m).ToString()
        if (nodeToReplace.Parent is MemberAccessExpressionSyntax or ConditionalAccessExpressionSyntax or ElementAccessExpressionSyntax or PostfixUnaryExpressionSyntax)
            return SyntaxFactory.ParenthesizedExpression(newNode);

        // a-(decimal)-1.5 => a- -1.5m, as a--1.5m would be a decrement
        if (!nodeToReplace.HasLeadingTrivia && nodeToReplace.GetFirstToken().GetPreviousToken() is { TrailingTrivia.Count: 0 } previousToken && previousToken.Kind() is SyntaxKind.MinusToken or SyntaxKind.MinusMinusToken)
            return newNode.WithLeadingTrivia(SyntaxFactory.Space);

        return newNode;
    }

    private static SyntaxNode UnwrapParentheses(SyntaxNode node)
    {
        while (node is ParenthesizedExpressionSyntax parenthesizedExpression)
        {
            node = parenthesizedExpression.Expression;
        }

        return node;
    }
}
