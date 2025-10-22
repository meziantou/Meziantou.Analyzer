using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseJSRuntimeInvokeVoidAsyncWhenReturnValueIsNotUsedFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseJSRuntimeInvokeVoidAsyncWhenReturnValueIsNotUsed);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not InvocationExpressionSyntax invocation)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use InvokeVoidAsync",
                ct => Update(context.Document, invocation, ct),
                equivalenceKey: "Use InvokeVoidAsync"),
            context.Diagnostics);
    }

    private static async Task<Document> Update(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        if (invocation.Expression is MemberAccessExpressionSyntax member)
        {
            if (member.Name.Identifier.ValueText.EndsWith("Async", System.StringComparison.Ordinal))
            {
                editor.ReplaceNode(member.Name, SyntaxFactory.IdentifierName("InvokeVoidAsync"));
            }
            else
            {
                editor.ReplaceNode(member.Name, SyntaxFactory.IdentifierName("InvokeVoid"));
            }
        }

        return editor.GetChangedDocument();
    }
}
