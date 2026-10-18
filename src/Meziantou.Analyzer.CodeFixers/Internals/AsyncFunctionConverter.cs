using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Makes async the function that contains an <c>await</c> expression added by a code fix: the method, the local function,
/// or the anonymous function. The return type of the methods and the local functions is replaced by a task, and the
/// return statements of the functions that already return a task are updated.
/// </summary>
internal static class AsyncFunctionConverter
{
    private static readonly SyntaxAnnotation FunctionAnnotation = new(nameof(AsyncFunctionConverter));

    /// <summary>
    /// Gets the function that contains <paramref name="node"/>. <paramref name="function"/> is <see langword="null"/> when the
    /// node is in a top-level statement, which can already await. Returns <see langword="false"/> when the node is in a
    /// member that cannot be async, such as a constructor, an accessor, an operator, or an initializer.
    /// </summary>
    public static bool TryGetEnclosingFunction(SyntaxNode node, out SyntaxNode? function)
    {
        foreach (var ancestor in node.Ancestors())
        {
            switch (ancestor)
            {
                case AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MethodDeclarationSyntax:
                    function = ancestor;
                    return true;

                case GlobalStatementSyntax:
                    function = null;
                    return true;

                case MemberDeclarationSyntax:
                    function = null;
                    return false;
            }
        }

        function = null;
        return false;
    }

    public static bool IsAsync(SyntaxNode function)
    {
        return function switch
        {
            MethodDeclarationSyntax method => method.Modifiers.Any(SyntaxKind.AsyncKeyword),
            LocalFunctionStatementSyntax localFunction => localFunction.Modifiers.Any(SyntaxKind.AsyncKeyword),
            AnonymousFunctionExpressionSyntax anonymousFunction => anonymousFunction.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword),
            _ => false,
        };
    }

    /// <summary>
    /// Makes <paramref name="function"/> async. <paramref name="replacedNode"/> is the node of the function that the code fix
    /// replaced by an await expression, so it cannot be replaced again.
    /// </summary>
    public static void MakeAsync(DocumentEditor editor, SyntaxNode function, SyntaxNode replacedNode, CancellationToken cancellationToken)
    {
        var semanticModel = editor.SemanticModel;
        var compilation = semanticModel.Compilation;
        var methodSymbol = function is AnonymousFunctionExpressionSyntax
            ? semanticModel.GetSymbolInfo(function, cancellationToken).Symbol as IMethodSymbol
            : semanticModel.GetDeclaredSymbol(function, cancellationToken) as IMethodSymbol;

        var taskTypes = new TaskTypes(compilation);

        TypeSyntax? newReturnType = null;
        if (methodSymbol is not null && taskTypes.TaskSymbol is not null)
        {
            switch (taskTypes.GetKind(methodSymbol.ReturnType))
            {
                case TaskKind.Task:
                    UpdateReturnStatements(editor, function, replacedNode, hasResult: false, taskTypes, cancellationToken);
                    break;

                case TaskKind.TaskOfT:
                    UpdateReturnStatements(editor, function, replacedNode, hasResult: true, taskTypes, cancellationToken);
                    break;

                // The return type of an anonymous function is inferred from its body, so the async function returns a task
                case TaskKind.None when function is not AnonymousFunctionExpressionSyntax:
                    newReturnType = CreateTaskType(editor.Generator, taskTypes.TaskSymbol, methodSymbol.ReturnsVoid ? null : GetReturnType(function));
                    break;
            }
        }

        editor.ReplaceNode(function, (currentNode, _) =>
        {
            var newNode = currentNode switch
            {
                MethodDeclarationSyntax method => AddAsyncModifier(method, newReturnType),
                LocalFunctionStatementSyntax localFunction => AddAsyncModifier(localFunction, newReturnType),
                SimpleLambdaExpressionSyntax lambda => lambda.WithoutLeadingTrivia().WithModifiers(AddAsyncModifier(lambda.Modifiers)).WithLeadingTrivia(lambda.GetLeadingTrivia()),
                ParenthesizedLambdaExpressionSyntax lambda => lambda.WithoutLeadingTrivia().WithModifiers(AddAsyncModifier(lambda.Modifiers)).WithLeadingTrivia(lambda.GetLeadingTrivia()),
                AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.WithoutLeadingTrivia().WithModifiers(AddAsyncModifier(anonymousMethod.Modifiers)).WithLeadingTrivia(anonymousMethod.GetLeadingTrivia()),
                _ => currentNode,
            };

            return newNode.WithAdditionalAnnotations(FunctionAnnotation);
        });
    }

    /// <summary>
    /// Making the function async can change the overload resolution of the anonymous functions, and the return type of the
    /// methods, which is used by their callers. The code fix is only valid when it does not introduce compiler errors.
    /// </summary>
    public static async Task<bool> IsValidAsync(Document document, Document fixedDocument, SyntaxNode function, CancellationToken cancellationToken)
    {
        var fixedRoot = await fixedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var fixedSemanticModel = await fixedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (fixedRoot is null || fixedSemanticModel is null)
            return false;

        // An async anonymous function converted to a delegate that returns void is an async void method, whose exceptions cannot be observed
        var fixedFunction = fixedRoot.GetAnnotatedNodes(FunctionAnnotation).FirstOrDefault();
        if (fixedFunction is null || fixedFunction is AnonymousFunctionExpressionSyntax && fixedSemanticModel.GetSymbolInfo(fixedFunction, cancellationToken).Symbol is not IMethodSymbol { ReturnsVoid: false })
            return false;

        var documentIds = new HashSet<DocumentId> { document.Id };
        if (function is MethodDeclarationSyntax)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel?.GetDeclaredSymbol(function, cancellationToken) is not { } methodSymbol)
                return false;

            // The callers of the method can be in the other documents of the project
            var references = await SymbolFinder.FindReferencesAsync(methodSymbol, document.Project.Solution, ImmutableHashSet.CreateRange(document.Project.Documents), cancellationToken).ConfigureAwait(false);
            foreach (var location in references.SelectMany(reference => reference.Locations))
            {
                documentIds.Add(location.Document.Id);
            }
        }

        foreach (var documentId in documentIds)
        {
            var errorCount = await GetErrorCountAsync(document.Project.Solution.GetDocument(documentId), cancellationToken).ConfigureAwait(false);
            var fixedErrorCount = await GetErrorCountAsync(fixedDocument.Project.Solution.GetDocument(documentId), cancellationToken).ConfigureAwait(false);
            if (fixedErrorCount > errorCount)
                return false;
        }

        return true;
    }

    private static async Task<int> GetErrorCountAsync(Document? document, CancellationToken cancellationToken)
    {
        if (document is null)
            return 0;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return 0;

        return semanticModel.GetDiagnostics(cancellationToken: cancellationToken).Count(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error);
    }

    private static TypeSyntax? GetReturnType(SyntaxNode function)
    {
        return function switch
        {
            MethodDeclarationSyntax method => method.ReturnType,
            LocalFunctionStatementSyntax localFunction => localFunction.ReturnType,
            _ => null,
        };
    }

    /// <summary>Creates <c>Task</c>, or <c>Task&lt;T&gt;</c> when the function returns a value, keeping the syntax of its return type.</summary>
    private static TypeSyntax CreateTaskType(SyntaxGenerator generator, INamedTypeSymbol taskSymbol, TypeSyntax? returnType)
    {
        if (returnType is null)
            return (TypeSyntax)generator.TypeExpression(taskSymbol);

        var taskType = generator.QualifiedName(generator.DottedName("System.Threading.Tasks"), generator.GenericName("Task", returnType.WithoutTrivia()));
        return (TypeSyntax)taskType.WithAdditionalAnnotations(Simplifier.Annotation);
    }

    private static MethodDeclarationSyntax AddAsyncModifier(MethodDeclarationSyntax method, TypeSyntax? newReturnType)
    {
        if (newReturnType is not null)
        {
            method = method.WithReturnType(newReturnType.WithTriviaFrom(method.ReturnType));
        }

        // Without modifier, the indentation is the leading trivia of the return type
        if (method.Modifiers.Count == 0)
            return method.WithReturnType(method.ReturnType.WithoutLeadingTrivia()).WithModifiers(TokenList(Token(method.ReturnType.GetLeadingTrivia(), SyntaxKind.AsyncKeyword, TriviaList(Space))));

        return method.WithModifiers(method.Modifiers.Add(Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(Space)));
    }

    private static LocalFunctionStatementSyntax AddAsyncModifier(LocalFunctionStatementSyntax localFunction, TypeSyntax? newReturnType)
    {
        if (newReturnType is not null)
        {
            localFunction = localFunction.WithReturnType(newReturnType.WithTriviaFrom(localFunction.ReturnType));
        }

        // Without modifier, the indentation is the leading trivia of the return type
        if (localFunction.Modifiers.Count == 0)
            return localFunction.WithReturnType(localFunction.ReturnType.WithoutLeadingTrivia()).WithModifiers(TokenList(Token(localFunction.ReturnType.GetLeadingTrivia(), SyntaxKind.AsyncKeyword, TriviaList(Space))));

        return localFunction.WithModifiers(localFunction.Modifiers.Add(Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(Space)));
    }

    // The leading trivia of the anonymous function is moved to the async keyword by the caller, so the keyword is the first modifier
    private static SyntaxTokenList AddAsyncModifier(SyntaxTokenList modifiers) => modifiers.Insert(0, Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(Space));

    /// <summary>
    /// The return statements of an async function return the result of the task instead of the task:
    /// <c>return task;</c> becomes <c>return await task;</c>, or <c>await task;</c> when the task has no result.
    /// </summary>
    private static void UpdateReturnStatements(DocumentEditor editor, SyntaxNode function, SyntaxNode replacedNode, bool hasResult, TaskTypes taskTypes, CancellationToken cancellationToken)
    {
        var semanticModel = editor.SemanticModel;
        var body = function switch
        {
            MethodDeclarationSyntax method => (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
            LocalFunctionStatementSyntax localFunction => (SyntaxNode?)localFunction.Body ?? localFunction.ExpressionBody?.Expression,
            AnonymousFunctionExpressionSyntax anonymousFunction => anonymousFunction.Body,
            _ => null,
        };

        if (body is null)
            return;

        if (body is ExpressionSyntax expressionBody)
        {
            if (expressionBody != replacedNode)
            {
                var getResult = hasResult && taskTypes.IsTaskFromResult(semanticModel.GetOperation(expressionBody, cancellationToken));
                editor.ReplaceNode(expressionBody, (currentNode, generator) => getResult ? GetTaskResult((ExpressionSyntax)currentNode) : generator.AwaitExpression(currentNode));
            }

            return;
        }

        // The return statements of the nested functions return from these functions
        var returnStatements = body.DescendantNodes(node => node == body || node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)).OfType<ReturnStatementSyntax>();
        foreach (var returnStatement in returnStatements)
        {
            if (returnStatement.Expression is not { } expression || expression == replacedNode)
                continue;

            if (hasResult)
            {
                var getResult = taskTypes.IsTaskFromResult(semanticModel.GetOperation(expression, cancellationToken));
                editor.ReplaceNode(returnStatement, (currentNode, generator) =>
                {
                    var currentReturnStatement = (ReturnStatementSyntax)currentNode;
                    var currentExpression = currentReturnStatement.Expression!;
                    return currentReturnStatement.WithExpression(getResult ? GetTaskResult(currentExpression) : (ExpressionSyntax)generator.AwaitExpression(currentExpression));
                });

                continue;
            }

            var isLastStatement = returnStatement.Parent == body && ((BlockSyntax)body).Statements.Last() == returnStatement;
            if (taskTypes.IsCompletedTask(semanticModel.GetOperation(expression, cancellationToken)))
            {
                // return Task.CompletedTask; => return;
                if (isLastStatement)
                {
                    editor.RemoveNode(returnStatement);
                }
                else
                {
                    editor.ReplaceNode(returnStatement, ReturnStatement().WithTriviaFrom(returnStatement));
                }

                continue;
            }

            // return task; => await task; return;
            if (isLastStatement)
            {
                editor.ReplaceNode(returnStatement, (currentNode, generator) => CreateAwaitStatement(generator, (ReturnStatementSyntax)currentNode));
            }
            else if (returnStatement.Parent is BlockSyntax or SwitchSectionSyntax)
            {
                editor.InsertAfter(returnStatement, ReturnStatement().WithAdditionalAnnotations(Formatter.Annotation));
                editor.ReplaceNode(returnStatement, (currentNode, generator) => CreateAwaitStatement(generator, (ReturnStatementSyntax)currentNode));
            }
            else
            {
                editor.ReplaceNode(returnStatement, (currentNode, generator) => Block(CreateAwaitStatement(generator, (ReturnStatementSyntax)currentNode).WithoutTrivia(), ReturnStatement())
                    .WithTriviaFrom(currentNode)
                    .WithAdditionalAnnotations(Formatter.Annotation));
            }
        }

        static StatementSyntax CreateAwaitStatement(SyntaxGenerator generator, ReturnStatementSyntax returnStatement)
        {
            return ExpressionStatement((ExpressionSyntax)generator.AwaitExpression(returnStatement.Expression!)).WithTriviaFrom(returnStatement);
        }
    }

    private static ExpressionSyntax GetTaskResult(ExpressionSyntax expression)
    {
        var arguments = expression switch
        {
            InvocationExpressionSyntax invocation => invocation.ArgumentList.Arguments,
            BaseObjectCreationExpressionSyntax { ArgumentList: { } argumentList } => argumentList.Arguments,
            _ => default,
        };

        return arguments.Count is 1 ? arguments[0].Expression.WithTriviaFrom(expression) : expression;
    }

    private enum TaskKind
    {
        None,
        Task,
        TaskOfT,
        AsyncIterator,
    }

    private sealed class TaskTypes(Compilation compilation)
    {
        private readonly INamedTypeSymbol? _taskOfTSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        private readonly INamedTypeSymbol? _valueTaskSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        private readonly INamedTypeSymbol? _valueTaskOfTSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        private readonly INamedTypeSymbol? _asyncEnumerableSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        private readonly INamedTypeSymbol? _asyncEnumeratorSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1");
        private readonly INamedTypeSymbol? _asyncMethodBuilderAttributeSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.AsyncMethodBuilderAttribute");

        public INamedTypeSymbol? TaskSymbol { get; } = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");

        public TaskKind GetKind(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol namedType)
                return TaskKind.None;

            if (namedType.IsEqualToAny(TaskSymbol, _valueTaskSymbol))
                return TaskKind.Task;

            if (namedType.OriginalDefinition.IsEqualToAny(_taskOfTSymbol, _valueTaskOfTSymbol))
                return TaskKind.TaskOfT;

            if (namedType.OriginalDefinition.IsEqualToAny(_asyncEnumerableSymbol, _asyncEnumeratorSymbol))
                return TaskKind.AsyncIterator;

            if (_asyncMethodBuilderAttributeSymbol is not null && namedType.HasAttribute(_asyncMethodBuilderAttributeSymbol))
                return namedType.TypeArguments.Length is 1 ? TaskKind.TaskOfT : TaskKind.Task;

            return TaskKind.None;
        }

        /// <summary>Indicates whether the operation is <c>Task.FromResult(value)</c>, <c>ValueTask.FromResult(value)</c>, or <c>new ValueTask&lt;T&gt;(value)</c>.</summary>
        public bool IsTaskFromResult(IOperation? operation)
        {
            return operation?.UnwrapImplicitConversions() switch
            {
                IInvocationOperation { TargetMethod: { Name: nameof(Task.FromResult), ContainingType: var containingType }, Arguments.Length: 1, Syntax: InvocationExpressionSyntax } => containingType.IsEqualToAny(TaskSymbol, _valueTaskSymbol),
                IObjectCreationOperation { Constructor.Parameters: [var parameter], Type: INamedTypeSymbol { TypeArguments: [var resultType] } type, Syntax: BaseObjectCreationExpressionSyntax } => type.OriginalDefinition.IsEqualTo(_valueTaskOfTSymbol) && parameter.Type.IsEqualTo(resultType),
                _ => false,
            };
        }

        /// <summary>Indicates whether the operation is <c>Task.CompletedTask</c>, <c>ValueTask.CompletedTask</c>, <c>default(ValueTask)</c>, or <c>new ValueTask()</c>.</summary>
        public bool IsCompletedTask(IOperation? operation)
        {
            return operation?.UnwrapImplicitConversions() switch
            {
                IPropertyReferenceOperation { Property: { Name: nameof(Task.CompletedTask), IsStatic: true, ContainingType: var containingType } } => containingType.IsEqualToAny(TaskSymbol, _valueTaskSymbol),
                IDefaultValueOperation { Type: var type } => type.IsEqualTo(_valueTaskSymbol),
                IObjectCreationOperation { Arguments.Length: 0, Initializer: null, Type: var type } => type.IsEqualTo(_valueTaskSymbol),
                _ => false,
            };
        }
    }
}
