using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Meziantou.Analyzer.Internals;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class TaskInUsingFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.TaskInUsing);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        if (nodeToFix is not ExpressionSyntax expressionSyntax)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (!CanAwaitToDisposable(semanticModel, expressionSyntax, context.CancellationToken))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Await task",
                ct => AddAwait(context.Document, nodeToFix, ct),
                equivalenceKey: "Await task"),
            context.Diagnostics);
    }

    private static bool CanAwaitToDisposable(SemanticModel semanticModel, ExpressionSyntax expressionSyntax, CancellationToken cancellationToken)
    {
        var type = semanticModel.GetTypeInfo(expressionSyntax, cancellationToken).Type as INamedTypeSymbol;
        if (type is null)
            return false;

        var taskOfT = semanticModel.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");
        if (taskOfT is null || !type.OriginalDefinition.IsEqualTo(taskOfT) || type.TypeArguments.Length != 1)
            return false;

        var disposableType = semanticModel.Compilation.GetBestTypeByMetadataName("System.IDisposable");
        if (disposableType is null)
            return false;

        var awaitedType = type.TypeArguments[0];
        return awaitedType.IsOrImplements(disposableType);
    }

    private static async Task<Document> AddAwait(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var node = nodeToFix.WithoutTrivia();

        ExpressionSyntax awaitExpression = SyntaxFactory.AwaitExpression((ExpressionSyntax)node).WithTriviaFrom(nodeToFix);

        editor.ReplaceNode(nodeToFix, awaitExpression.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }
}
