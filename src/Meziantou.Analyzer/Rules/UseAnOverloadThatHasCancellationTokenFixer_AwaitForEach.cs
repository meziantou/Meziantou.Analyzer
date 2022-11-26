using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseAnOverloadThatHasCancellationTokenFixer_AwaitForEach : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.FlowCancellationTokenInAwaitForEachWhenACancellationTokenIsAvailable);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not ExpressionSyntax expressionSyntax)
            return;

        if (!context.Diagnostics[0].Properties.TryGetValue("CancellationTokens", out var cancellationTokens) || cancellationTokens == null)
            return;

        foreach (var cancellationToken in cancellationTokens.Split(','))
        {
            var title = "Use CancellationToken:  " + cancellationToken;
            var codeAction = CodeAction.Create(
                title,
                ct => FixInvocation(context.Document, expressionSyntax, cancellationToken, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static async Task<Document> FixInvocation(Document document, ExpressionSyntax expressionSyntax, string cancellationTokenExpression, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var expression = SyntaxFactory.ParseExpression(cancellationTokenExpression);
        var newExpression = generator.InvocationExpression(generator.MemberAccessExpression(expressionSyntax, "WithCancellation"), expression);
        editor.ReplaceNode(expressionSyntax, newExpression);
        return editor.GetChangedDocument();
    }
}
