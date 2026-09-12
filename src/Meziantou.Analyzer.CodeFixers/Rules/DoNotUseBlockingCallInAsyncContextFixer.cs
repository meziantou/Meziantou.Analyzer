using Microsoft.CodeAnalysis.Formatting;

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
                    var taskSymbol = sm?.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
                    if (taskSymbol is null)
                        break;

                    var codeAction = CodeAction.Create(
                        "Use Task.Delay",
                        ct => UseTaskDelay(context.Document, nodeToFix, taskSymbol, ct),
                        equivalenceKey: "Thread_Sleep");

                    context.RegisterCodeFix(codeAction, context.Diagnostics);
                    break;
                }

            case DoNotUseBlockingCallInAsyncContextData.Task_Wait:
                {
                    if (nodeToFix is not InvocationExpressionSyntax taskWaitInvocation ||
                        (taskWaitInvocation.Expression as MemberAccessExpressionSyntax)?.Expression is null)
                        break;

                    var codeAction = CodeAction.Create(
                        "Use await",
                        ct => ReplaceTaskWaitWithAwait(context.Document, nodeToFix, ct),
                        equivalenceKey: "Task_Wait");

                    context.RegisterCodeFix(codeAction, context.Diagnostics);
                    break;
                }

            case DoNotUseBlockingCallInAsyncContextData.Task_Result:
                {
                    var codeAction = CodeAction.Create(
                        "Use await",
                        ct => ReplaceTaskResultWithAwait(context.Document, nodeToFix, ct),
                        equivalenceKey: "Task_Result");

                    context.RegisterCodeFix(codeAction, context.Diagnostics);
                    break;
                }

            case DoNotUseBlockingCallInAsyncContextData.Overload:
                {
                    if (!properties.TryGetValue(DoNotUseBlockingCallInAsyncContextAnalyzerCommon.MethodNameKey, out var methodName) || methodName is null)
                        return;

                    properties.TryGetValue(DoNotUseBlockingCallInAsyncContextAnalyzerCommon.NamespaceToImportKey, out var namespaceToImport);
                    var codeAction = CodeAction.Create(
                        $"Use '{methodName}'",
                        ct => ReplaceWithMethodName(context.Document, nodeToFix, methodName, namespaceToImport, ct),
                        equivalenceKey: "Overload");

                    context.RegisterCodeFix(codeAction, context.Diagnostics);
                    break;
                }

            case DoNotUseBlockingCallInAsyncContextData.Using:
            case DoNotUseBlockingCallInAsyncContextData.UsingDeclarator:
                {
                    var codeAction = CodeAction.Create(
                        $"Use 'await using'",
                        ct => ReplaceWithAwaitUsing(context.Document, nodeToFix, ct),
                        equivalenceKey: "Overload");

                    context.RegisterCodeFix(codeAction, context.Diagnostics);
                    break;
                }
        }
    }

    private static async Task<Document> ReplaceWithAwaitUsing(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
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

        return editor.GetChangedDocument();
    }

    private static SyntaxToken GetAwaitKeyword(SyntaxToken usingKeyword)
    {
        return SyntaxFactory.Token(usingKeyword.LeadingTrivia, SyntaxKind.AwaitKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space));
    }

    private static async Task<Document> ReplaceWithMethodName(Document document, SyntaxNode nodeToFix, string methodName, string? namespaceToImport, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var invocation = (InvocationExpressionSyntax)nodeToFix;
        var nodeToReplace = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            IdentifierNameSyntax identifier => identifier,
            _ => null,
        };

        if (nodeToReplace is null)
            return document;

        var newMethodName = nodeToReplace switch
        {
            GenericNameSyntax genericName => generator.GenericName(methodName, genericName.TypeArgumentList.Arguments),
            _ => generator.IdentifierName(methodName),
        };
        var newNode = nodeToFix.ReplaceNode(nodeToReplace, newMethodName);

        // An extension method cannot be called with a simple name, so the implicit receiver must be explicit: Do() => this.DoAsync()
        if (nodeToReplace == invocation.Expression && !editor.SemanticModel.LookupSymbols(invocation.SpanStart, name: methodName).OfType<IMethodSymbol>().Any())
        {
            newNode = invocation.WithExpression((ExpressionSyntax)generator.MemberAccessExpression(generator.ThisExpression(), newMethodName));
        }

        var newExpression = generator.AwaitExpression(newNode).Parenthesize();
        editor.ReplaceNode(nodeToFix, newExpression);

        if (namespaceToImport is not null && !IsNamespaceImported(nodeToFix, namespaceToImport))
        {
            editor.ReplaceNode(GetUsingDirectivesContainer(nodeToFix), (container, _) => AddUsingDirective(container, namespaceToImport));
        }

        return editor.GetChangedDocument();
    }

    private static bool IsNamespaceImported(SyntaxNode node, string namespaceName)
    {
        foreach (var ancestor in node.Ancestors())
        {
            var usings = ancestor switch
            {
                CompilationUnitSyntax compilationUnit => compilationUnit.Usings,
                BaseNamespaceDeclarationSyntax namespaceDeclaration => namespaceDeclaration.Usings,
                _ => default,
            };

            foreach (var usingDirective in usings)
            {
                if (IsNamespaceUsingDirective(usingDirective) && usingDirective.Name?.ToString() == namespaceName)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the node where the using directive is added: the innermost namespace declaration containing the node that already has using
    /// directives, or the compilation unit.
    /// </summary>
    private static SyntaxNode GetUsingDirectivesContainer(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is BaseNamespaceDeclarationSyntax { Usings.Count: > 0 })
                return ancestor;

            if (ancestor is CompilationUnitSyntax)
                return ancestor;
        }

        throw new InvalidOperationException("The node is not in a compilation unit");
    }

    private static SyntaxNode AddUsingDirective(SyntaxNode container, string namespaceName)
    {
        var endOfLine = container.DescendantTrivia().FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));
        if (endOfLine.IsKind(SyntaxKind.None))
        {
            endOfLine = SyntaxFactory.ElasticCarriageReturnLineFeed;
        }

        var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
            .NormalizeWhitespace()
            .WithTrailingTrivia(endOfLine)
            .WithAdditionalAnnotations(Formatter.Annotation);

        switch (container)
        {
            case CompilationUnitSyntax { Usings.Count: 0, Externs.Count: 0, Members: [var firstMember, ..] } compilationUnit:
                // The using directive becomes the first node of the file, so it takes the leading trivia of the first member, such as a file header
                usingDirective = usingDirective.WithLeadingTrivia(firstMember.GetLeadingTrivia()).WithTrailingTrivia(endOfLine, endOfLine);
                return compilationUnit
                    .ReplaceNode(firstMember, firstMember.WithoutLeadingTrivia())
                    .WithUsings(SyntaxFactory.SingletonList(usingDirective));

            case CompilationUnitSyntax compilationUnit:
                return compilationUnit.WithUsings(InsertUsingDirective(compilationUnit.Usings, usingDirective));

            case BaseNamespaceDeclarationSyntax namespaceDeclaration:
                return namespaceDeclaration.WithUsings(InsertUsingDirective(namespaceDeclaration.Usings, usingDirective));

            default:
                return container;
        }
    }

    /// <summary>
    /// Inserts the using directive with the other namespace using directives, at its sorted position when they are sorted
    /// (<c>System</c> namespaces first), or after them otherwise.
    /// </summary>
    private static SyntaxList<UsingDirectiveSyntax> InsertUsingDirective(SyntaxList<UsingDirectiveSyntax> usings, UsingDirectiveSyntax usingDirective)
    {
        var namespaceName = usingDirective.Name!.ToString();
        var firstIndex = -1;
        var lastIndex = -1;
        var isSorted = true;
        string? previousName = null;
        for (var i = 0; i < usings.Count; i++)
        {
            if (!IsNamespaceUsingDirective(usings[i]))
                continue;

            var name = usings[i].Name!.ToString();
            if (previousName is not null && CompareNamespaces(previousName, name) > 0)
            {
                isSorted = false;
            }

            if (firstIndex < 0)
            {
                firstIndex = i;
            }

            lastIndex = i;
            previousName = name;
        }

        int index;
        if (lastIndex < 0)
        {
            // Global using directives must be before the other using directives
            index = 0;
            while (index < usings.Count && !usings[index].GlobalKeyword.IsKind(SyntaxKind.None))
            {
                index++;
            }
        }
        else
        {
            index = lastIndex + 1;
            if (isSorted)
            {
                for (var i = firstIndex; i <= lastIndex; i++)
                {
                    if (IsNamespaceUsingDirective(usings[i]) && CompareNamespaces(namespaceName, usings[i].Name!.ToString()) < 0)
                    {
                        index = i;
                        break;
                    }
                }
            }
        }

        // Keep the leading trivia, such as a file header, at the beginning of the list
        if (index == 0 && usings.Count > 0)
        {
            usingDirective = usingDirective.WithLeadingTrivia(usings[0].GetLeadingTrivia());
            usings = usings.Replace(usings[0], usings[0].WithoutLeadingTrivia());
        }

        return usings.Insert(index, usingDirective);
    }

    private static bool IsNamespaceUsingDirective(UsingDirectiveSyntax usingDirective)
    {
        return usingDirective.GlobalKeyword.IsKind(SyntaxKind.None)
            && usingDirective.StaticKeyword.IsKind(SyntaxKind.None)
            && usingDirective.Alias is null
            && usingDirective.Name is not null;
    }

    private static int CompareNamespaces(string x, string y)
    {
        var xIsSystem = IsSystemNamespace(x);
        var yIsSystem = IsSystemNamespace(y);
        if (xIsSystem != yIsSystem)
            return xIsSystem ? -1 : 1;

        return StringComparer.OrdinalIgnoreCase.Compare(x, y);

        static bool IsSystemNamespace(string name) => name == "System" || name.StartsWith("System.", StringComparison.Ordinal);
    }

    private static async Task<Document> ReplaceTaskResultWithAwait(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var expr = ((MemberAccessExpressionSyntax)nodeToFix).Expression;
        var newExpression = generator.AwaitExpression(expr).Parenthesize();
        editor.ReplaceNode(nodeToFix, newExpression);

        return editor.GetChangedDocument();
    }

    private static async Task<Document> ReplaceTaskWaitWithAwait(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var invocation = (InvocationExpressionSyntax)nodeToFix;
        var expr = (invocation.Expression as MemberAccessExpressionSyntax)!.Expression;
        var newExpression = generator.AwaitExpression(expr).Parenthesize();
        editor.ReplaceNode(nodeToFix, newExpression);

        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseTaskDelay(Document document, SyntaxNode nodeToFix, INamedTypeSymbol taskSymbol, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var invocation = (InvocationExpressionSyntax)nodeToFix;
        var delay = invocation.ArgumentList.Arguments[0].Expression;

        var newExpression = generator.AwaitExpression(generator.InvocationExpression(generator.MemberAccessExpression(generator.TypeExpression(taskSymbol), "Delay"), delay)).Parenthesize();
        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }
}
