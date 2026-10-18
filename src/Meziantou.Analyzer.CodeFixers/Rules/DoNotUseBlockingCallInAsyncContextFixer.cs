using Microsoft.CodeAnalysis.Simplification;

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
        if (nodeToFix is null)
            return;

        var properties = context.Diagnostics[0].Properties;
        if (!properties.TryGetValue(DoNotUseBlockingCallInAsyncContextAnalyzerCommon.DataKey, out var dataStr) || !Enum.TryParse<DoNotUseBlockingCallInAsyncContextData>(dataStr, ignoreCase: false, out var data))
            return;

        switch (data)
        {
            case DoNotUseBlockingCallInAsyncContextData.Thread_Sleep:
                {
                    var sm = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                    var taskSymbol = sm?.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
                    if (taskSymbol is null)
                        break;

                    if (nodeToFix is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: > 0 } threadSleepInvocation)
                        break;

                    await RegisterCodeFixAsync(context, threadSleepInvocation, "Use Task.Delay", "Thread_Sleep", editor => UseTaskDelay(editor, threadSleepInvocation, taskSymbol)).ConfigureAwait(false);
                    break;
                }

            case DoNotUseBlockingCallInAsyncContextData.Task_Wait:
                {
                    if (nodeToFix is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax } taskWaitInvocation)
                        break;

                    await RegisterCodeFixAsync(context, taskWaitInvocation, "Use await", "Task_Wait", editor => ReplaceTaskWaitWithAwait(editor, taskWaitInvocation)).ConfigureAwait(false);
                    break;
                }

            case DoNotUseBlockingCallInAsyncContextData.Task_Result:
                {
                    // The conditional access (t?.Result) and the property patterns (t is { Result: 1 }) cannot be replaced by an await expression
                    if (nodeToFix is not MemberAccessExpressionSyntax taskResultMemberAccess || nodeToFix.FirstAncestorOrSelf<SubpatternSyntax>() is not null)
                        break;

                    await RegisterCodeFixAsync(context, taskResultMemberAccess, "Use await", "Task_Result", editor => ReplaceTaskResultWithAwait(editor, taskResultMemberAccess)).ConfigureAwait(false);
                    break;
                }

            case DoNotUseBlockingCallInAsyncContextData.Overload:
                {
                    if (!properties.TryGetValue(DoNotUseBlockingCallInAsyncContextAnalyzerCommon.MethodNameKey, out var methodName) || methodName is null)
                        return;

                    // The conditional access (c?.Write()) cannot be awaited without changing the behavior when the instance is null
                    if (nodeToFix is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax or SimpleNameSyntax } overloadInvocation)
                        return;

                    var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                    if (semanticModel is null)
                        return;

                    properties.TryGetValue(OverloadFinder.NamespaceToImportPropertyName, out var namespaceToImport);

                    // The overload may not be callable with the same arguments: Write() => Write(CancellationToken), or A(in value) => AAsync(int value).
                    // The extension method of a namespace that is not imported yet cannot be bound before the using directive is added.
                    if (namespaceToImport is null)
                    {
                        var renamedInvocation = CreateRenamedInvocation(SyntaxGenerator.GetGenerator(context.Document), semanticModel, overloadInvocation, methodName);
                        var renamedMethod = semanticModel.GetSpeculativeSymbolInfo(overloadInvocation.SpanStart, renamedInvocation, SpeculativeBindingOption.BindAsExpression).Symbol;
                        if (renamedMethod is not IMethodSymbol { ReturnsVoid: false } || renamedMethod.IsEqualTo(semanticModel.GetSymbolInfo(overloadInvocation, context.CancellationToken).Symbol))
                            return;
                    }

                    await RegisterCodeFixAsync(context, overloadInvocation, $"Use '{methodName}'", "Overload", editor => ReplaceWithMethodName(editor, overloadInvocation, methodName, namespaceToImport)).ConfigureAwait(false);
                    break;
                }

            case DoNotUseBlockingCallInAsyncContextData.Using:
            case DoNotUseBlockingCallInAsyncContextData.UsingDeclarator:
                {
                    await RegisterCodeFixAsync(context, nodeToFix, "Use 'await using'", "Overload", editor => ReplaceWithAwaitUsing(editor, nodeToFix)).ConfigureAwait(false);
                    break;
                }
        }
    }

    /// <summary>
    /// Registers a code fix that replaces the blocking call by an await expression. The enclosing function must be async,
    /// and making it async can break the code, so the fix is computed upfront and only registered when the code still compiles.
    /// </summary>
    private static async Task RegisterCodeFixAsync(CodeFixContext context, SyntaxNode nodeToFix, string title, string equivalenceKey, Action<DocumentEditor> fix)
    {
        var document = context.Document;
        if (!AsyncFunctionConverter.TryGetEnclosingFunction(nodeToFix, out var function))
            return;

        if (function is null || AsyncFunctionConverter.IsAsync(function))
        {
            context.RegisterCodeFix(CodeAction.Create(title, ct => FixAsync(document, nodeToFix, function: null, fix, ct), equivalenceKey), context.Diagnostics);
            return;
        }

        // The await expressions are parenthesized, and the parentheses are only removed by the simplifier: (await task);
        var fixedDocument = await FixAsync(document, nodeToFix, function, fix, context.CancellationToken).ConfigureAwait(false);
        fixedDocument = await Simplifier.ReduceAsync(fixedDocument, Simplifier.Annotation, cancellationToken: context.CancellationToken).ConfigureAwait(false);
        if (!await AsyncFunctionConverter.IsValidAsync(document, fixedDocument, function, context.CancellationToken).ConfigureAwait(false))
            return;

        context.RegisterCodeFix(CodeAction.Create(title, _ => Task.FromResult(fixedDocument), equivalenceKey), context.Diagnostics);
    }

    private static async Task<Document> FixAsync(Document document, SyntaxNode nodeToFix, SyntaxNode? function, Action<DocumentEditor> fix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        fix(editor);
        if (function is not null)
        {
            AsyncFunctionConverter.MakeAsync(editor, function, nodeToFix, cancellationToken);
        }

        return editor.GetChangedDocument();
    }

    private static void ReplaceWithAwaitUsing(DocumentEditor editor, SyntaxNode nodeToFix)
    {
        if (nodeToFix is UsingStatementSyntax usingStatement)
        {
            var awaitKeyword = GetAwaitKeyword(usingStatement.UsingKeyword);
            editor.ReplaceNode(
                usingStatement,
                usingStatement.WithUsingKeyword(usingStatement.UsingKeyword.WithLeadingTrivia(SyntaxFactory.TriviaList())).WithAwaitKeyword(awaitKeyword));
        }
        else if (nodeToFix is LocalDeclarationStatementSyntax localDeclarationStatement)
        {
            var awaitKeyword = GetAwaitKeyword(localDeclarationStatement.UsingKeyword);
            editor.ReplaceNode(
                localDeclarationStatement,
                localDeclarationStatement.WithUsingKeyword(localDeclarationStatement.UsingKeyword.WithLeadingTrivia(SyntaxFactory.TriviaList())).WithAwaitKeyword(awaitKeyword));
        }
    }

    private static SyntaxToken GetAwaitKeyword(SyntaxToken usingKeyword)
    {
        return SyntaxFactory.Token(usingKeyword.LeadingTrivia, SyntaxKind.AwaitKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space));
    }

    private static void ReplaceWithMethodName(DocumentEditor editor, InvocationExpressionSyntax invocation, string methodName, string? namespaceToImport)
    {
        var newNode = CreateRenamedInvocation(editor.Generator, editor.SemanticModel, invocation, methodName);
        var newExpression = editor.Generator.AwaitExpression(newNode).Parenthesize();
        editor.ReplaceNode(invocation, newExpression);

        if (namespaceToImport is not null)
        {
            UsingDirectiveHelper.AddUsingDirective(editor, invocation, namespaceToImport);
        }
    }

    private static InvocationExpressionSyntax CreateRenamedInvocation(SyntaxGenerator generator, SemanticModel semanticModel, InvocationExpressionSyntax invocation, string methodName)
    {
        var nodeToReplace = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            _ => (SimpleNameSyntax)invocation.Expression,
        };

        var newMethodName = nodeToReplace switch
        {
            GenericNameSyntax genericName => generator.GenericName(methodName, genericName.TypeArgumentList.Arguments),
            _ => generator.IdentifierName(methodName),
        };

        // An extension method cannot be called with a simple name, so the implicit receiver must be explicit: Do() => this.DoAsync()
        if (nodeToReplace == invocation.Expression && !semanticModel.LookupSymbols(invocation.SpanStart, name: methodName).OfType<IMethodSymbol>().Any())
            return invocation.WithExpression((ExpressionSyntax)generator.MemberAccessExpression(generator.ThisExpression(), newMethodName));

        return invocation.ReplaceNode(nodeToReplace, newMethodName);
    }

    private static void ReplaceTaskResultWithAwait(DocumentEditor editor, MemberAccessExpressionSyntax memberAccess)
    {
        var newExpression = editor.Generator.AwaitExpression(memberAccess.Expression).Parenthesize();
        editor.ReplaceNode(memberAccess, newExpression);
    }

    private static void ReplaceTaskWaitWithAwait(DocumentEditor editor, InvocationExpressionSyntax invocation)
    {
        var expr = ((MemberAccessExpressionSyntax)invocation.Expression).Expression;
        var newExpression = editor.Generator.AwaitExpression(expr).Parenthesize();
        editor.ReplaceNode(invocation, newExpression);
    }

    private static void UseTaskDelay(DocumentEditor editor, InvocationExpressionSyntax invocation, INamedTypeSymbol taskSymbol)
    {
        var generator = editor.Generator;
        var delay = invocation.ArgumentList.Arguments[0].Expression;

        var newExpression = generator.AwaitExpression(generator.InvocationExpression(generator.TypeMemberAccessExpression(taskSymbol, "Delay"), delay)).Parenthesize();
        editor.ReplaceNode(invocation, newExpression);
    }
}
