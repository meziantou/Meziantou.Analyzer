using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class ReturnTaskFromResultInsteadOfReturningNullFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.ReturnTaskFromResultInsteadOfReturningNull);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix == null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return;

        if (ReturnTaskFromResultInsteadOfReturningNullAnalyzer.FindContainingMethod(semanticModel, nodeToFix, context.CancellationToken)?.ReturnType is not INamedTypeSymbol type)
            return;

        if (!type.IsGenericType)
        {
            var title = "Use Task.CompletedTask";
            context.RegisterCodeFix(CodeAction.Create(title, ct => UseTaskCompleted(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
        }
        else
        {
            var title = "Use Task.FromResult";
            context.RegisterCodeFix(CodeAction.Create(title, ct => UseTaskFromResult(context.Document, nodeToFix, type, ct), equivalenceKey: title), context.Diagnostics);
        }
    }

    private static async Task<Document> UseTaskCompleted(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var typeSymbol = editor.SemanticModel.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
        if (typeSymbol == null)
            return document;

        var newExpression = generator.MemberAccessExpression(generator.TypeExpression(typeSymbol), nameof(Task.CompletedTask));

        if (nodeToFix is ReturnStatementSyntax { Expression: { } } returnStatementSyntax)
        {
            editor.ReplaceNode(returnStatementSyntax.Expression, newExpression);
        }
        else
        {
            editor.ReplaceNode(nodeToFix, newExpression);
        }

        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseTaskFromResult(Document document, SyntaxNode nodeToFix, INamedTypeSymbol typeSymbol, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var taskTypeSymbol = editor.SemanticModel.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
        if (taskTypeSymbol == null)
            return document;

        var newExpression = generator.MemberAccessExpression(generator.TypeExpression(taskTypeSymbol), generator.GenericName("FromResult", typeSymbol.TypeArguments[0]));
        newExpression = generator.InvocationExpression(newExpression, generator.DefaultExpression(typeSymbol.TypeArguments[0]));

        if (nodeToFix is ReturnStatementSyntax { Expression: { } } returnStatementSyntax)
        {
            editor.ReplaceNode(returnStatementSyntax.Expression, newExpression);
        }
        else
        {
            editor.ReplaceNode(nodeToFix, newExpression);
        }

        return editor.GetChangedDocument();
    }
}
