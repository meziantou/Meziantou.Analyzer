using System;
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
public sealed class DoNotUseBlockingCallInAsyncContextFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotUseBlockingCallInAsyncContext, RuleIdentifiers.DoNotUseBlockingCall);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix == null)
            return;

        var properties = context.Diagnostics[0].Properties;
        if (!properties.TryGetValue("Data", out var dataStr) || !Enum.TryParse<DoNotUseBlockingCallInAsyncContextData>(dataStr, out var data))
            return;

        switch (data)
        {
            case DoNotUseBlockingCallInAsyncContextData.Thread_Sleep:
                {
                    var codeAction = CodeAction.Create(
                        "Use Task.Delay",
                        ct => UseTaskDelay(context.Document, nodeToFix, ct),
                        equivalenceKey: "Thread_Sleep");

                    context.RegisterCodeFix(codeAction, context.Diagnostics);
                }
                break;

            case DoNotUseBlockingCallInAsyncContextData.Task_Wait:
                {
                    var codeAction = CodeAction.Create(
                        "Use await",
                        ct => ReplaceTaskWaitWithAwait(context.Document, nodeToFix, ct),
                        equivalenceKey: "Task_Wait");

                    context.RegisterCodeFix(codeAction, context.Diagnostics);
                }
                break;

            case DoNotUseBlockingCallInAsyncContextData.Task_Result:
                {
                    var codeAction = CodeAction.Create(
                        "Use await",
                        ct => ReplaceTaskResultWithAwait(context.Document, nodeToFix, ct),
                        equivalenceKey: "Task_Result");

                    context.RegisterCodeFix(codeAction, context.Diagnostics);
                }
                break;

            case DoNotUseBlockingCallInAsyncContextData.Overload:
                {
                    if (!properties.TryGetValue("MethodName", out var methodName) || methodName == null)
                        return;

                    var codeAction = CodeAction.Create(
                        $"Use '{methodName}'",
                        ct => ReplaceWithMethodName(context.Document, nodeToFix, methodName, ct),
                        equivalenceKey: "Overload");

                    context.RegisterCodeFix(codeAction, context.Diagnostics);
                }
                break;

            case DoNotUseBlockingCallInAsyncContextData.Using:
            case DoNotUseBlockingCallInAsyncContextData.UsingDeclarator:
                {
                    var codeAction = CodeAction.Create(
                        $"Use 'await using'",
                        ct => ReplaceWithAwaitUsing(context.Document, nodeToFix, ct),
                        equivalenceKey: "Overload");

                    context.RegisterCodeFix(codeAction, context.Diagnostics);
                }
                break;
        }
    }

    private static async Task<Document> ReplaceWithAwaitUsing(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (nodeToFix is UsingStatementSyntax usingStatement)
        {
            editor.ReplaceNode(usingStatement, usingStatement.WithAwaitKeyword(SyntaxFactory.Token(SyntaxKind.AwaitKeyword)));
        }
        else if(nodeToFix is LocalDeclarationStatementSyntax localDeclarationStatement)
        {
            editor.ReplaceNode(localDeclarationStatement, localDeclarationStatement.WithAwaitKeyword(SyntaxFactory.Token(SyntaxKind.AwaitKeyword)));
        }

        return editor.GetChangedDocument();
    }

    private static async Task<Document> ReplaceWithMethodName(Document document, SyntaxNode nodeToFix, string methodName, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var invocation = (InvocationExpressionSyntax)nodeToFix;
        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        if (memberAccess == null)
            return document;

        var newNode = nodeToFix.ReplaceNode(memberAccess.Name, generator.IdentifierName(methodName));
        var newExpression = generator.AwaitExpression(newNode);
        editor.ReplaceNode(nodeToFix, newExpression);

        return editor.GetChangedDocument();
    }

    private static async Task<Document> ReplaceTaskResultWithAwait(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var expr = ((MemberAccessExpressionSyntax)nodeToFix).Expression;
        if (expr == null)
            return document;

        var newExpression = generator.AwaitExpression(expr);
        editor.ReplaceNode(nodeToFix, newExpression);

        return editor.GetChangedDocument();
    }

    private static async Task<Document> ReplaceTaskWaitWithAwait(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var invocation = (InvocationExpressionSyntax)nodeToFix;
        var expr = (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;
        if (expr == null)
            return document;

        var newExpression = generator.AwaitExpression(expr);
        editor.ReplaceNode(nodeToFix, newExpression);

        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseTaskDelay(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var taskSymbol = editor.SemanticModel.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
        if (taskSymbol == null)
            return document;

        var invocation = (InvocationExpressionSyntax)nodeToFix;
        var delay = invocation.ArgumentList.Arguments[0].Expression;

        var newExpression = generator.AwaitExpression(generator.InvocationExpression(generator.MemberAccessExpression(generator.TypeExpression(taskSymbol), "Delay"), delay));
        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }
}
