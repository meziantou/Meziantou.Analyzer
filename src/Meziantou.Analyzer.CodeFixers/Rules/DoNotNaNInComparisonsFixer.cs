namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class DoNotNaNInComparisonsFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotNaNInComparisons);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var binaryExpression = nodeToFix.FirstAncestorOrSelf<BinaryExpressionSyntax>();
        if (binaryExpression is null)
            return;

        var binaryOperation = semanticModel.GetOperation(binaryExpression, context.CancellationToken) as IBinaryOperation;
        if (binaryOperation is null)
            return;

        // Only the equality comparisons can be rewritten to IsNaN.
        // The relational comparisons are always false, so there is no equivalent expression to suggest.
        if (binaryOperation.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals))
            return;

        var leftIsNaN = TryGetNaNType(binaryOperation.LeftOperand, semanticModel.Compilation, out _, out var leftSyntax);
        var rightIsNaN = TryGetNaNType(binaryOperation.RightOperand, semanticModel.Compilation, out _, out var rightSyntax);

        // NaN == NaN is false and NaN != NaN is true, whereas IsNaN(NaN) is true.
        // Replacing the comparison would change the behavior of the code, so there is nothing to fix.
        if (leftIsNaN && rightIsNaN)
            return;

        if (leftIsNaN && leftSyntax is not null && nodeToFix.IsEquivalentTo(leftSyntax))
        {
            RegisterCodeFix(binaryOperation.LeftOperand);
        }

        if (rightIsNaN && rightSyntax is not null && nodeToFix.IsEquivalentTo(rightSyntax))
        {
            RegisterCodeFix(binaryOperation.RightOperand);
        }

        void RegisterCodeFix(IOperation nanOperand)
        {
            var title = "Use IsNaN";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => FixComparison(context.Document, binaryOperation, nanOperand, ct),
                    equivalenceKey: title),
                context.Diagnostics);
        }
    }

    private static async Task<Document> FixComparison(Document document, IBinaryOperation binaryOperation, IOperation nanOperand, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (!TryGetReplacementExpression(editor.Generator, binaryOperation, nanOperand, editor.SemanticModel.Compilation, out var replacement))
            return document;

        editor.ReplaceNode(binaryOperation.Syntax, replacement.WithTriviaFrom(binaryOperation.Syntax));
        return editor.GetChangedDocument();
    }

    private static bool TryGetReplacementExpression(SyntaxGenerator generator, IBinaryOperation binaryOperation, IOperation nanOperand, Compilation compilation, out ExpressionSyntax replacement)
    {
        var leftIsNaN = TryGetNaNType(binaryOperation.LeftOperand, compilation, out var leftType, out _);
        var rightIsNaN = TryGetNaNType(binaryOperation.RightOperand, compilation, out var rightType, out _);
        if (!leftIsNaN && !rightIsNaN)
        {
            replacement = null!;
            return false;
        }

        IOperation otherOperand;
        if (nanOperand.Syntax.IsEquivalentTo(binaryOperation.LeftOperand.Syntax))
        {
            otherOperand = binaryOperation.RightOperand;
        }
        else if (nanOperand.Syntax.IsEquivalentTo(binaryOperation.RightOperand.Syntax))
        {
            otherOperand = binaryOperation.LeftOperand;
        }
        else
        {
            replacement = null!;
            return false;
        }

        var nanType = leftType ?? rightType;
        if (nanType is null)
        {
            replacement = null!;
            return false;
        }

        var isNaNInvocation = (ExpressionSyntax)generator.InvocationExpression(
            generator.TypeMemberAccessExpression(nanType, "IsNaN", addImport: true),
            otherOperand.Syntax);

        replacement = binaryOperation.OperatorKind == BinaryOperatorKind.NotEquals
            ? SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, isNaNInvocation.Parenthesize())
            : isNaNInvocation;

        return true;
    }

    private static bool TryGetNaNType(IOperation operation, Compilation compilation, out ITypeSymbol? typeSymbol, out ExpressionSyntax? expression)
    {
        while (operation is IConversionOperation conversionOperation)
        {
            operation = conversionOperation.Operand;
        }

        if (operation is IMemberReferenceOperation memberReference &&
            memberReference.Member is ISymbol { Name: "NaN", ContainingType: { } containingType })
        {
            if (containingType.SpecialType is SpecialType.System_Double or SpecialType.System_Single)
            {
                typeSymbol = containingType;
                expression = memberReference.Syntax as ExpressionSyntax;
                return true;
            }

            if (compilation.GetBestTypeByMetadataName("System.Half") is { } halfTypeSymbol && containingType.IsEqualTo(halfTypeSymbol))
            {
                typeSymbol = containingType;
                expression = memberReference.Syntax as ExpressionSyntax;
                return true;
            }

            // Generic math: T.NaN where T is constrained to IFloatingPointIeee754<T>. The member is the one of the
            // interface, so the type to call IsNaN on is its type argument, which is the type written in the code.
            if (containingType is { TypeArguments: [{ } selfType] } &&
                containingType.OriginalDefinition.IsEqualTo(compilation.GetBestTypeByMetadataName("System.Numerics.IFloatingPointIeee754`1")))
            {
                typeSymbol = selfType;
                expression = memberReference.Syntax as ExpressionSyntax;
                return true;
            }
        }

        typeSymbol = null;
        expression = null;
        return false;
    }
}
