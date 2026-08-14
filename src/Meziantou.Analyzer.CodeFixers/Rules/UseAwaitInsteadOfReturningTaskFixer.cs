using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
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
        if (nodeToFix is null)
            return;

        var function = nodeToFix.FirstAncestorOrSelf<SyntaxNode>(IsFunction);
        if (function is null)
            return;

        const string Title = "Use await";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => FixAsync(context.Document, nodeToFix, function, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> FixAsync(Document document, SyntaxNode nodeToFix, SyntaxNode function, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var semanticModel = editor.SemanticModel;

        if (nodeToFix is not ExpressionSyntax value)
            return document;

        var isGeneric = IsGenericTaskLike(semanticModel.Compilation, semanticModel.GetTypeInfo(value, cancellationToken).ConvertedType);

        var awaitExpression = ((ExpressionSyntax)generator.AwaitExpression(value.WithoutTrivia())).WithTriviaFrom(value);

        SyntaxNode newFunction;
        if (value.Parent is ReturnStatementSyntax returnStatement && !isGeneric)
        {
            // "return X;" cannot be turned into "return await X;" for a non-generic task: drop the return
            var expressionStatement = ExpressionStatement(awaitExpression).WithTriviaFrom(returnStatement);
            newFunction = function.ReplaceNode(returnStatement, expressionStatement);
        }
        else
        {
            newFunction = function.ReplaceNode(value, awaitExpression);
        }

        newFunction = AddAsyncModifier(newFunction, generator);
        editor.ReplaceNode(function, newFunction.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static SyntaxNode AddAsyncModifier(SyntaxNode function, SyntaxGenerator generator)
    {
        switch (function)
        {
            case MethodDeclarationSyntax:
            case LocalFunctionStatementSyntax:
                return generator.WithModifiers(function, generator.GetModifiers(function).WithAsync(isAsync: true));

            case ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax or AnonymousMethodExpressionSyntax:
                {
                    var leadingTrivia = function.GetLeadingTrivia();
                    var asyncKeyword = Token(SyntaxKind.AsyncKeyword)
                        .WithLeadingTrivia(leadingTrivia)
                        .WithTrailingTrivia(Space);

                    return function switch
                    {
                        ParenthesizedLambdaExpressionSyntax lambda => lambda.WithoutLeadingTrivia().WithAsyncKeyword(asyncKeyword),
                        SimpleLambdaExpressionSyntax lambda => lambda.WithoutLeadingTrivia().WithAsyncKeyword(asyncKeyword),
                        AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.WithoutLeadingTrivia().WithAsyncKeyword(asyncKeyword),
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
}
