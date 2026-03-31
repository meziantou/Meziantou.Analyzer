using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;

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
            generator.MemberAccessExpression(operation.LeftOperand.Syntax, "SequenceEqual"), operation.RightOperand.Syntax);

        if (operation.OperatorKind == BinaryOperatorKind.NotEquals)
        {
            newExpression = generator.LogicalNotExpression(newExpression);
        }

        editor.ReplaceNode(operation.Syntax, newExpression.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }
}
