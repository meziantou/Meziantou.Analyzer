using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class DoNotUseStringGetHashCodeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotUseStringGetHashCode);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not InvocationExpressionSyntax nodeToFix)
            return;

        if (nodeToFix.Expression is not MemberAccessExpressionSyntax)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var stringComparerSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.StringComparer");
        if (stringComparerSymbol is null)
            return;

        var title = "Use StringComparer.Ordinal";
        var codeAction = CodeAction.Create(
            title,
            ct => AddStringComparison(context.Document, nodeToFix, stringComparerSymbol, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> AddStringComparison(Document document, SyntaxNode nodeToFix, INamedTypeSymbol stringComparer, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var invocationExpression = (InvocationExpressionSyntax)nodeToFix;
        var memberAccessExpression = (MemberAccessExpressionSyntax)invocationExpression.Expression;

        var newExpression = generator.InvocationExpression(
            generator.MemberAccessExpression(
                generator.MemberAccessExpression(
                    generator.TypeExpression(stringComparer, addImport: true),
                    nameof(StringComparer.Ordinal)),
                nameof(StringComparer.GetHashCode)),
            memberAccessExpression.Expression);

        editor.ReplaceNode(invocationExpression, newExpression);
        return editor.GetChangedDocument();
    }
}
