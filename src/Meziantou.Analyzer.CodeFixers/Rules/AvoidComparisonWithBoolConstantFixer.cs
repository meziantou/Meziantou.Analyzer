using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class AvoidComparisonWithBoolConstantFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AvoidComparisonWithBoolConstant);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not BinaryExpressionSyntax)
            return;

        var properties = context.Diagnostics[0].Properties;
        if (!properties.TryGetValue(AvoidComparisonWithBoolConstantAnalyzerCommon.NodeToKeepSpanStartKey, out var nodeToKeepSpanStartValue) ||
            !int.TryParse(nodeToKeepSpanStartValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var nodeToKeepSpanStart))
            return;

        if (!properties.TryGetValue(AvoidComparisonWithBoolConstantAnalyzerCommon.NodeToKeepSpanLengthKey, out var nodeToKeepSpanLengthValue) ||
            !int.TryParse(nodeToKeepSpanLengthValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var nodeToKeepSpanLength))
            return;

        if (!properties.TryGetValue(AvoidComparisonWithBoolConstantAnalyzerCommon.LogicalNotOperatorNeededKey, out var logicalNotOperatorNeededValue) ||
            !bool.TryParse(logicalNotOperatorNeededValue, out var logicalNotOperatorNeeded))
            return;

        var title = "Remove comparison with bool constant";
        var codeAction = CodeAction.Create(
            title,
            ct => RemoveComparisonWithBoolConstant(context.Document, nodeToFix, new TextSpan(nodeToKeepSpanStart, nodeToKeepSpanLength), logicalNotOperatorNeeded, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> RemoveComparisonWithBoolConstant(Document document, SyntaxNode nodeToFix, TextSpan nodeToKeepSpan, bool logicalNotOperatorNeeded, CancellationToken cancellationToken)
    {
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
            return document;

        var nodeToKeep = syntaxRoot.FindNode(nodeToKeepSpan, getInnermostNodeForTie: true);
        if (nodeToKeep.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
        {
            nodeToKeep = nodeToKeep.Parent;
        }

        if (logicalNotOperatorNeeded)
        {
            // The operand may bind less tightly than '!' (e.g. "o is string" or "a < b"), so it must be parenthesized.
            // The Simplifier annotation removes the parentheses when they are redundant.
            var operand = nodeToKeep is ParenthesizedExpressionSyntax parenthesizedExpression ? parenthesizedExpression : ((ExpressionSyntax)nodeToKeep).Parenthesize();
            nodeToKeep = PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, operand);
        }

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        editor.ReplaceNode(nodeToFix, nodeToKeep.WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation));

        return editor.GetChangedDocument();
    }
}
