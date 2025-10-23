using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis;
using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseIsPatternInsteadOfSequenceEqualFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseIsPatternInsteadOfSequenceEqual);

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

        var title = "Use 'is' pattern";
        context.RegisterCodeFix(CodeAction.Create(title, ct => UseIs(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
    }

    private static async Task<Document> UseIs(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (editor.SemanticModel.GetOperation(nodeToFix, cancellationToken) is not IInvocationOperation operation)
            return document;

        var newExpression = SyntaxFactory.IsPatternExpression((ExpressionSyntax)operation.Arguments[0].Value.Syntax, SyntaxFactory.ConstantPattern((ExpressionSyntax)operation.Arguments[1].Value.Syntax));
        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }
}
