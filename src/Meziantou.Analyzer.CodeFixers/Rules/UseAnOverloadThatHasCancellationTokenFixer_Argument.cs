using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseAnOverloadThatHasCancellationTokenFixer_Argument : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailable);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        if (nodeToFix.IsKind(SyntaxKind.InvocationExpression))
        {
            if (!int.TryParse(context.Diagnostics[0].Properties["ParameterIndex"], NumberStyles.None, CultureInfo.InvariantCulture, out var parameterIndex))
                return;

            if (!context.Diagnostics[0].Properties.TryGetValue("ParameterName", out var parameterName) || parameterName is null)
                return;

            if (!context.Diagnostics[0].Properties.TryGetValue("ParameterIsEnumeratorCancellation", out var parameterIsEnumeratorCancellation) || !bool.TryParse(parameterIsEnumeratorCancellation, out var isEnumeratorCancellation))
                return;

            if (!context.Diagnostics[0].Properties.TryGetValue("CancellationTokens", out var cancellationTokens) || cancellationTokens is null)
                return;

            foreach (var cancellationToken in cancellationTokens.Split(','))
            {
                var title = "Use CancellationToken:  " + cancellationToken;
                var codeAction = CodeAction.Create(
                    title,
                    ct => FixInvocation(context.Document, (InvocationExpressionSyntax)nodeToFix, parameterIndex, parameterName, cancellationToken, isEnumeratorCancellation, ct),
                    equivalenceKey: title);

                context.RegisterCodeFix(codeAction, context.Diagnostics);
            }
        }
    }

    private static async Task<Document> FixInvocation(Document document, InvocationExpressionSyntax nodeToFix, int index, string parameterName, string cancellationTokenText, bool isEnumeratorCancellation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var cancellationTokenExpression = SyntaxFactory.ParseExpression(cancellationTokenText);

        SyntaxNode? parentNode = null;
        if (isEnumeratorCancellation)
        {
            if (editor.SemanticModel.GetOperation(nodeToFix, cancellationToken) is IInvocationOperation invocation)
            {
                // Check direct invocation parent
                if (invocation.Parent?.Parent is IInvocationOperation { TargetMethod.Name: "WithCancellation", Arguments: [{ }, { Value: var withCancellationArgument }] } parent)
                {
                    if (withCancellationArgument.Syntax.IsEquivalentTo(cancellationTokenExpression))
                    {
                        parentNode = parent.Syntax;
                    }
                }
            }
        }

        SyntaxNode newInvocation;
        if (index > nodeToFix.ArgumentList.Arguments.Count)
        {
            var newArguments = nodeToFix.ArgumentList.Arguments.Add((ArgumentSyntax)generator.Argument(parameterName, RefKind.None, cancellationTokenExpression));
            newInvocation = nodeToFix.WithArgumentList(SyntaxFactory.ArgumentList(newArguments));
        }
        else
        {
            var newArguments = nodeToFix.ArgumentList.Arguments.Insert(index, (ArgumentSyntax)generator.Argument(cancellationTokenExpression));
            newInvocation = nodeToFix.WithArgumentList(SyntaxFactory.ArgumentList(newArguments));
        }

        if (parentNode is not null)
        {
            editor.ReplaceNode(parentNode, newInvocation);
        }
        else
        {
            editor.ReplaceNode(nodeToFix, newInvocation);
        }

        return editor.GetChangedDocument();
    }
}
