using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseStringComparerFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseStringComparer);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        // In case the target expression is wrapped in another node with the same span,
        // get the innermost node for ties.
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        if (!CanFix(nodeToFix))
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var stringComparerSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.StringComparer");
        if (stringComparerSymbol is null)
            return;

#if CSHARP_PREVIEW
        if (nodeToFix is CollectionExpressionSyntax collectionExpression && !CanFixCollectionExpression(collectionExpression))
            return;
#endif

        RegisterCodeFix(nameof(StringComparer.Ordinal));
        RegisterCodeFix(nameof(StringComparer.OrdinalIgnoreCase));

        void RegisterCodeFix(string comparerName)
        {
            var title = "Add StringComparer." + comparerName;
            var codeAction = CodeAction.Create(
                title,
                ct => AddStringComparer(context.Document, nodeToFix, comparerName, stringComparerSymbol, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static async Task<Document> AddStringComparer(Document document, SyntaxNode nodeToFix, string comparerName, INamedTypeSymbol stringComparer, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var newArgument = (ArgumentSyntax)generator.Argument(
            generator.MemberAccessExpression(
                generator.TypeExpression(stringComparer, addImport: true),
                comparerName));

        switch (nodeToFix)
        {
            case ObjectCreationExpressionSyntax creationExpression:
                editor.ReplaceNode(creationExpression, AddArgument(creationExpression, newArgument));
                break;

            case ImplicitObjectCreationExpressionSyntax implicitCreationExpression:
                editor.ReplaceNode(implicitCreationExpression, implicitCreationExpression.AddArgumentListArguments(newArgument));
                break;

            case InvocationExpressionSyntax invocationExpression:
                editor.ReplaceNode(invocationExpression, invocationExpression.AddArgumentListArguments(newArgument));
                break;

#if CSHARP_PREVIEW
            case CollectionExpressionSyntax collectionExpression:
                editor.ReplaceNode(collectionExpression, AddCollectionArgument(collectionExpression, stringComparer, comparerName));
                break;
#endif

            default:
                return document;
        }

        return editor.GetChangedDocument();
    }

    private static ObjectCreationExpressionSyntax AddArgument(ObjectCreationExpressionSyntax creationExpression, ArgumentSyntax argument)
    {
        if (creationExpression.ArgumentList is not null)
            return creationExpression.AddArgumentListArguments(argument);

        var trailingTrivia = creationExpression.Type.GetTrailingTrivia();
        return creationExpression
            .WithType(creationExpression.Type.WithoutTrailingTrivia())
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(argument)).WithTrailingTrivia(trailingTrivia));
    }

    private static bool CanFix(SyntaxNode nodeToFix)
    {
        return nodeToFix is ObjectCreationExpressionSyntax
            or ImplicitObjectCreationExpressionSyntax
            or InvocationExpressionSyntax
#if CSHARP_PREVIEW
            or CollectionExpressionSyntax
#endif
            ;
    }

#if CSHARP_PREVIEW
    private static bool CanFixCollectionExpression(CollectionExpressionSyntax collectionExpression)
    {
        return collectionExpression.Elements.Count == 0;
    }

    private static CollectionExpressionSyntax AddCollectionArgument(CollectionExpressionSyntax collectionExpression, INamedTypeSymbol stringComparer, string comparerName)
    {
        var comparerType = stringComparer.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var replacement = SyntaxFactory.ParseExpression($"[with({comparerType}.{comparerName})]");
        return replacement.WithTriviaFrom(collectionExpression) as CollectionExpressionSyntax ?? collectionExpression;
    }
#endif
}
