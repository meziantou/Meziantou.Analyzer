using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseAwaitInsteadOfReturningTaskFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseAwaitInsteadOfReturningTask);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not ExpressionSyntax)
            return;

        if (nodeToFix.FirstAncestorOrSelf<SyntaxNode>(IsFunction) is null)
            return;

        const string Title = "Use await";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => FixAsync(context.Document, nodeToFix, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> FixAsync(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;

        if (nodeToFix is not ExpressionSyntax value)
            return document;

        var function = value.FirstAncestorOrSelf<SyntaxNode>(IsFunction);
        if (function is null)
            return document;

        var isGeneric = IsGenericTaskLike(semanticModel.Compilation, semanticModel.GetTypeInfo(value, cancellationToken).ConvertedType);

        var (expressionBody, block) = GetBody(function);

        SyntaxNode newFunction;
        if (expressionBody is not null)
        {
            newFunction = function.ReplaceNode(expressionBody, MakeAwait(expressionBody));
        }
        else if (block is not null)
        {
            var tail = block.Statements.LastOrDefault() as ReturnStatementSyntax;
            if (tail is { Expression: null })
                tail = null;

            var rewriter = new AwaitReturnRewriter(isGeneric, tail);
            newFunction = function.ReplaceNode(block, rewriter.Visit(block));
        }
        else
        {
            return document;
        }

        newFunction = AddAsyncModifier(newFunction);
        editor.ReplaceNode(function, newFunction.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static (ExpressionSyntax? ExpressionBody, BlockSyntax? Block) GetBody(SyntaxNode function)
    {
        return function switch
        {
            MethodDeclarationSyntax method => (method.ExpressionBody?.Expression, method.Body),
            LocalFunctionStatementSyntax localFunction => (localFunction.ExpressionBody?.Expression, localFunction.Body),
            ParenthesizedLambdaExpressionSyntax lambda => lambda.Body is BlockSyntax lambdaBlock ? (null, lambdaBlock) : (lambda.Body as ExpressionSyntax, null),
            SimpleLambdaExpressionSyntax lambda => lambda.Body is BlockSyntax lambdaBlock ? (null, lambdaBlock) : (lambda.Body as ExpressionSyntax, null),
            AnonymousMethodExpressionSyntax anonymousMethod => (null, anonymousMethod.Block),
            _ => (null, null),
        };
    }

    private static AwaitExpressionSyntax MakeAwait(ExpressionSyntax expression)
    {
        var operand = ((ExpressionSyntax)expression.WithoutTrivia()).Parentheses();
        return AwaitExpression(Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(Space), operand).WithTriviaFrom(expression);
    }

    private static SyntaxNode AddAsyncModifier(SyntaxNode function)
    {
        var asyncKeyword = Token(SyntaxKind.AsyncKeyword);

        switch (function)
        {
            case MethodDeclarationSyntax method:
                return method.Modifiers.Count > 0
                    ? method.WithModifiers(method.Modifiers.Add(asyncKeyword.WithTrailingTrivia(Space)))
                    : method.WithReturnType(method.ReturnType.WithoutLeadingTrivia())
                        .WithModifiers(TokenList(asyncKeyword.WithLeadingTrivia(method.ReturnType.GetLeadingTrivia()).WithTrailingTrivia(Space)));

            case LocalFunctionStatementSyntax localFunction:
                return localFunction.Modifiers.Count > 0
                    ? localFunction.WithModifiers(localFunction.Modifiers.Add(asyncKeyword.WithTrailingTrivia(Space)))
                    : localFunction.WithReturnType(localFunction.ReturnType.WithoutLeadingTrivia())
                        .WithModifiers(TokenList(asyncKeyword.WithLeadingTrivia(localFunction.ReturnType.GetLeadingTrivia()).WithTrailingTrivia(Space)));

            case ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax or AnonymousMethodExpressionSyntax:
                {
                    var lambdaAsyncKeyword = asyncKeyword
                        .WithLeadingTrivia(function.GetLeadingTrivia())
                        .WithTrailingTrivia(Space);

                    return function switch
                    {
                        ParenthesizedLambdaExpressionSyntax lambda => lambda.WithoutLeadingTrivia().WithAsyncKeyword(lambdaAsyncKeyword),
                        SimpleLambdaExpressionSyntax lambda => lambda.WithoutLeadingTrivia().WithAsyncKeyword(lambdaAsyncKeyword),
                        AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.WithoutLeadingTrivia().WithAsyncKeyword(lambdaAsyncKeyword),
                        _ => function,
                    };
                }

            default:
                return function;
        }
    }

    private static bool IsGenericTaskLike(Compilation compilation, ITypeSymbol? symbol)
    {
        if (symbol is not INamedTypeSymbol { IsGenericType: true } namedType)
            return false;

        var taskOfT = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskOfT = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        if (namedType.OriginalDefinition.IsEqualToAny(taskOfT, valueTaskOfT))
            return true;

        var asyncMethodBuilder = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.AsyncMethodBuilderAttribute");
        if (asyncMethodBuilder is not null && namedType.HasAttribute(asyncMethodBuilder))
            return true;

        return false;
    }

    private static bool IsFunction(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax or LocalFunctionStatementSyntax or ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax or AnonymousMethodExpressionSyntax;
    }

    private sealed class AwaitReturnRewriter : CSharpSyntaxRewriter
    {
        private readonly bool _isGeneric;
        private readonly ReturnStatementSyntax? _tailReturn;

        public AwaitReturnRewriter(bool isGeneric, ReturnStatementSyntax? tailReturn)
        {
            _isGeneric = isGeneric;
            _tailReturn = tailReturn;
        }

        // Do not descend into nested functions
        public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) => node;

        public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) => node;

        public override SyntaxNode? VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) => node;

        public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) => node;

        public override SyntaxNode? VisitBlock(BlockSyntax node)
        {
            var statements = new List<StatementSyntax>();
            foreach (var statement in node.Statements)
            {
                if (statement is ReturnStatementSyntax { Expression: not null } returnStatement)
                {
                    statements.AddRange(Expand(returnStatement));
                }
                else if (Visit(statement) is StatementSyntax visitedStatement)
                {
                    statements.Add(visitedStatement);
                }
            }

            return node.WithStatements(List(statements));
        }

        public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
        {
            // Only reached for embedded returns (e.g. "if (c) return X;"); block children are handled in VisitBlock
            if (node.Expression is null)
                return base.VisitReturnStatement(node);

            var expanded = Expand(node).ToList();
            return expanded.Count == 1 ? expanded[0] : Block(expanded);
        }

        private IEnumerable<StatementSyntax> Expand(ReturnStatementSyntax returnStatement)
        {
            var awaitExpression = MakeAwait(returnStatement.Expression!);

            if (_isGeneric)
            {
                yield return ReturnStatement(awaitExpression).WithTriviaFrom(returnStatement);
            }
            else if (returnStatement == _tailReturn)
            {
                yield return ExpressionStatement(awaitExpression).WithTriviaFrom(returnStatement);
            }
            else
            {
                yield return ExpressionStatement(awaitExpression);
                yield return ReturnStatement();
            }
        }
    }
}
