using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseContainsKeyInsteadOfTryGetValueFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseContainsKeyInsteadOfTryGetValue);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        const string Title = "Use ContainsKey";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => UseContainsKey(context.Document, nodeToFix, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> UseContainsKey(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (FindInvocation(editor.SemanticModel, nodeToFix, cancellationToken) is not { Arguments.Length: 2 } operation)
            return document;

        if (operation.TargetMethod.Name != "TryGetValue")
            return document;

        if (operation.Arguments[1].Value is not IDiscardOperation)
            return document;

        if (operation.Syntax is not InvocationExpressionSyntax invocationSyntax ||
            invocationSyntax.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return document;
        }

        var newInvocation = invocationSyntax
            .WithExpression(memberAccess.WithName(IdentifierName("ContainsKey")))
            .WithArgumentList(ArgumentList(SeparatedList(new[] { invocationSyntax.ArgumentList.Arguments[0] })));

        editor.ReplaceNode(invocationSyntax, newInvocation.WithTriviaFrom(invocationSyntax).WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static IInvocationOperation? FindInvocation(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
    {
        foreach (var candidate in node.AncestorsAndSelf())
        {
            if (semanticModel.GetOperation(candidate, cancellationToken) is IInvocationOperation invocation)
                return invocation;
        }

        return null;
    }
}
