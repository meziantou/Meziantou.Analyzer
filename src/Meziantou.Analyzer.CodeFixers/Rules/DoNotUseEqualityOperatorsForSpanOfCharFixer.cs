using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class DoNotUseEqualityOperatorsForSpanOfCharFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotUseEqualityOperatorsForSpanOfChar);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (semanticModel.GetOperation(nodeToFix, context.CancellationToken) is not IBinaryOperation operation)
            return;

        var title = "Use SequenceEquals";
        var codeAction = CodeAction.Create(
            title,
            ct => Refactor(context.Document, operation, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> Refactor(Document document, IBinaryOperation operation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var newExpression = generator.InvocationExpression(
            generator.MemberAccessExpression(GetReceiver(generator, operation.LeftOperand), "SequenceEqual"), operation.RightOperand.Syntax);

        if (operation.OperatorKind == BinaryOperatorKind.NotEquals)
        {
            newExpression = generator.LogicalNotExpression(newExpression);
        }

        editor.ReplaceNode(operation.Syntax, newExpression.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    /// <summary>
    /// The receiver of an extension method is not implicitly converted to a span before C# 14, so the conversion of the
    /// left operand ("a" == span) must be explicit: "a".AsSpan().SequenceEqual(span).
    /// </summary>
    private static SyntaxNode GetReceiver(SyntaxGenerator generator, IOperation operand)
    {
        if (operand is not IConversionOperation { IsImplicit: true, Operand: var convertedOperand, Type: { } spanType })
            return operand.Syntax;

        if (convertedOperand.Type is { SpecialType: SpecialType.System_String })
            return generator.InvocationExpression(generator.MemberAccessExpression(convertedOperand.Syntax, "AsSpan"));

        return generator.CastExpression(spanType, convertedOperand.Syntax).WithAdditionalAnnotations(Simplifier.Annotation);
    }
}
