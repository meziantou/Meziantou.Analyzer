using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class AvoidClosureWhenUsingConcurrentDictionaryFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionary,
        RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionaryByUsingFactoryArg);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (!TryGetAnonymousFunctionOperation(semanticModel, nodeToFix, context.CancellationToken, out var lambdaOperation))
            return;

        if (!TryGetInvocationAndArgument(lambdaOperation, out var invocationOperation, out var lambdaArgument))
            return;

        if (context.Diagnostics.Any(d => d.Id == RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionary))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use lambda parameters",
                    ct => UseLambdaParameters(context.Document, semanticModel, invocationOperation, lambdaOperation, lambdaArgument, ct),
                    equivalenceKey: "Use lambda parameters"),
                context.Diagnostics);
        }

        if (context.Diagnostics.Any(d => d.Id == RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionaryByUsingFactoryArg))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use factoryArgument overload",
                    ct => UseFactoryArgumentOverload(context.Document, semanticModel, invocationOperation, lambdaOperation, ct),
                    equivalenceKey: "Use factoryArgument overload"),
                context.Diagnostics);
        }
    }

    private static async Task<Document> UseLambdaParameters(Document document, SemanticModel semanticModel, IInvocationOperation invocationOperation, IAnonymousFunctionOperation lambdaOperation, IArgumentOperation lambdaArgument, CancellationToken cancellationToken)
    {
        var mappings = GetReplacementMappings(invocationOperation, lambdaOperation, lambdaArgument);
        if (mappings.Count == 0)
            return document;

        var updatedLambda = (AnonymousFunctionExpressionSyntax)lambdaOperation.Syntax;
        foreach (var (symbol, parameterName) in mappings)
        {
            updatedLambda = ReplaceSymbolReferences(updatedLambda, semanticModel, symbol, parameterName);
        }

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(lambdaOperation.Syntax, updatedLambda.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseFactoryArgumentOverload(Document document, SemanticModel semanticModel, IInvocationOperation invocationOperation, IAnonymousFunctionOperation lambdaOperation, CancellationToken cancellationToken)
    {
        if (invocationOperation.Syntax is not InvocationExpressionSyntax invocationSyntax)
            return document;

        var capturedSymbol = GetCapturedSymbols(semanticModel, lambdaOperation).FirstOrDefault();
        if (capturedSymbol is not ILocalSymbol and not IParameterSymbol)
            return document;

        var newInvocation = invocationOperation.TargetMethod.Name switch
        {
            "GetOrAdd" => CreateGetOrAddInvocationWithFactoryArg(invocationOperation, invocationSyntax, lambdaOperation, capturedSymbol, semanticModel),
            "AddOrUpdate" => CreateAddOrUpdateInvocationWithFactoryArg(invocationOperation, invocationSyntax, capturedSymbol, semanticModel),
            _ => null,
        };

        if (newInvocation is null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(invocationSyntax, newInvocation.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static InvocationExpressionSyntax? CreateGetOrAddInvocationWithFactoryArg(IInvocationOperation invocationOperation, InvocationExpressionSyntax invocationSyntax, IAnonymousFunctionOperation lambdaOperation, ISymbol capturedSymbol, SemanticModel semanticModel)
    {
        if (invocationOperation.Arguments.Length != 2)
            return null;

        var lambda = lambdaOperation.Syntax as AnonymousFunctionExpressionSyntax;
        if (lambda is null)
            return null;

        var parameterName = GetUniqueParameterName(lambdaOperation.Symbol.Parameters.Select(p => p.Name), "arg");
        var updatedLambda = ReplaceSymbolReferences(lambda, semanticModel, capturedSymbol, parameterName);
        updatedLambda = AddParameterToLambda(updatedLambda, parameterName);
        if (updatedLambda is null)
            return null;

        var arguments = invocationSyntax.ArgumentList.Arguments;
        arguments = arguments.Replace(arguments[1], arguments[1].WithExpression(updatedLambda));
        arguments = arguments.Add(Argument(IdentifierName(capturedSymbol.Name)));
        return invocationSyntax.WithArgumentList(invocationSyntax.ArgumentList.WithArguments(arguments));
    }

    private static InvocationExpressionSyntax? CreateAddOrUpdateInvocationWithFactoryArg(IInvocationOperation invocationOperation, InvocationExpressionSyntax invocationSyntax, ISymbol capturedSymbol, SemanticModel semanticModel)
    {
        if (invocationOperation.Arguments.Length != 3)
            return null;

        if (!TryGetAnonymousFunctionOperation(invocationOperation.Arguments[1].Value, out var addValueFactoryOperation))
            return null;

        if (!TryGetAnonymousFunctionOperation(invocationOperation.Arguments[2].Value, out var updateValueFactoryOperation))
            return null;

        var addValueFactory = addValueFactoryOperation.Syntax as AnonymousFunctionExpressionSyntax;
        var updateValueFactory = updateValueFactoryOperation.Syntax as AnonymousFunctionExpressionSyntax;
        if (addValueFactory is null || updateValueFactory is null)
            return null;

        var parameterName = GetUniqueParameterName(
            addValueFactoryOperation.Symbol.Parameters.Select(p => p.Name).Concat(updateValueFactoryOperation.Symbol.Parameters.Select(p => p.Name)),
            "arg");

        addValueFactory = ReplaceSymbolReferences(addValueFactory, semanticModel, capturedSymbol, parameterName);
        addValueFactory = AddParameterToLambda(addValueFactory, parameterName);
        if (addValueFactory is null)
            return null;

        updateValueFactory = ReplaceSymbolReferences(updateValueFactory, semanticModel, capturedSymbol, parameterName);
        updateValueFactory = AddParameterToLambda(updateValueFactory, parameterName);
        if (updateValueFactory is null)
            return null;

        var arguments = invocationSyntax.ArgumentList.Arguments;
        arguments = arguments.Replace(arguments[1], arguments[1].WithExpression(addValueFactory));
        arguments = arguments.Replace(arguments[2], arguments[2].WithExpression(updateValueFactory));
        arguments = arguments.Add(Argument(IdentifierName(capturedSymbol.Name)));
        return invocationSyntax.WithArgumentList(invocationSyntax.ArgumentList.WithArguments(arguments));
    }

    private static List<(ISymbol Symbol, string ParameterName)> GetReplacementMappings(IInvocationOperation invocationOperation, IAnonymousFunctionOperation lambdaOperation, IArgumentOperation lambdaArgument)
    {
        var result = new List<(ISymbol Symbol, string ParameterName)>();
        var lambdaIndex = invocationOperation.Arguments.IndexOf(lambdaArgument);

        if (invocationOperation.TargetMethod.Name is "GetOrAdd")
        {
            if (invocationOperation.Arguments.Length == 2 && lambdaIndex == 1 && lambdaOperation.Symbol.Parameters.Length >= 1)
            {
                AddMapping(result, invocationOperation.Arguments[0], lambdaOperation.Symbol.Parameters[0].Name);
            }
            else if (invocationOperation.Arguments.Length == 3 && lambdaIndex == 1 && lambdaOperation.Symbol.Parameters.Length >= 2)
            {
                AddMapping(result, invocationOperation.Arguments[0], lambdaOperation.Symbol.Parameters[0].Name);
                AddMapping(result, invocationOperation.Arguments[2], lambdaOperation.Symbol.Parameters[1].Name);
            }
        }
        else if (invocationOperation.TargetMethod.Name is "AddOrUpdate")
        {
            if (invocationOperation.Arguments.Length == 3 && lambdaIndex == 1 && lambdaOperation.Symbol.Parameters.Length >= 1)
            {
                AddMapping(result, invocationOperation.Arguments[0], lambdaOperation.Symbol.Parameters[0].Name);
            }
            else if (invocationOperation.Arguments.Length == 3 && lambdaIndex == 2 && lambdaOperation.Symbol.Parameters.Length >= 1)
            {
                AddMapping(result, invocationOperation.Arguments[0], lambdaOperation.Symbol.Parameters[0].Name);
            }
            else if (invocationOperation.Arguments.Length == 4 && lambdaIndex == 1 && lambdaOperation.Symbol.Parameters.Length >= 2)
            {
                AddMapping(result, invocationOperation.Arguments[0], lambdaOperation.Symbol.Parameters[0].Name);
                AddMapping(result, invocationOperation.Arguments[3], lambdaOperation.Symbol.Parameters[1].Name);
            }
            else if (invocationOperation.Arguments.Length == 4 && lambdaIndex == 2 && lambdaOperation.Symbol.Parameters.Length >= 3)
            {
                AddMapping(result, invocationOperation.Arguments[0], lambdaOperation.Symbol.Parameters[0].Name);
                AddMapping(result, invocationOperation.Arguments[3], lambdaOperation.Symbol.Parameters[2].Name);
            }
        }

        return result;

        static void AddMapping(List<(ISymbol Symbol, string ParameterName)> mappings, IArgumentOperation argument, string parameterName)
        {
            if (TryGetLocalOrParameterSymbol(argument, out var symbol))
            {
                mappings.Add((symbol, parameterName));
            }
        }
    }

    private static IEnumerable<ISymbol> GetCapturedSymbols(SemanticModel semanticModel, IAnonymousFunctionOperation lambdaOperation)
    {
        var dataFlowNode = GetDataFlowArgument(lambdaOperation.Syntax);
        if (dataFlowNode is null)
            yield break;

        var dataFlow = semanticModel.AnalyzeDataFlow(dataFlowNode);
        var parameters = lambdaOperation.Symbol.Parameters;

        foreach (var symbol in dataFlow.CapturedInside)
        {
            if (!parameters.Contains(symbol, SymbolEqualityComparer.Default))
            {
                yield return symbol;
            }
        }
    }

    private static AnonymousFunctionExpressionSyntax ReplaceSymbolReferences(AnonymousFunctionExpressionSyntax lambda, SemanticModel semanticModel, ISymbol symbolToReplace, string replacementParameterName)
    {
        var rewriter = new SymbolReferenceRewriter(semanticModel, symbolToReplace, replacementParameterName);
        return (AnonymousFunctionExpressionSyntax)rewriter.Visit(lambda);
    }

    private static ParenthesizedLambdaExpressionSyntax? AddParameterToLambda(AnonymousFunctionExpressionSyntax lambda, string parameterName)
    {
        var parameter = Parameter(Identifier(parameterName));

        if (lambda is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
        {
            return parenthesizedLambda.WithParameterList(parenthesizedLambda.ParameterList.WithParameters(parenthesizedLambda.ParameterList.Parameters.Add(parameter)));
        }

        if (lambda is SimpleLambdaExpressionSyntax simpleLambda)
        {
            var parameters = SeparatedList(new[] { simpleLambda.Parameter, parameter });

            ParenthesizedLambdaExpressionSyntax updatedLambda;
            if (simpleLambda.Block is not null)
            {
                updatedLambda = ParenthesizedLambdaExpression(ParameterList(parameters), simpleLambda.Block);
            }
            else if (simpleLambda.ExpressionBody is not null)
            {
                updatedLambda = ParenthesizedLambdaExpression(ParameterList(parameters), simpleLambda.ExpressionBody);
            }
            else
            {
                return null;
            }

            return updatedLambda.WithAsyncKeyword(simpleLambda.AsyncKeyword);
        }

        return null;
    }

    private static string GetUniqueParameterName(IEnumerable<string> existingParameterNames, string baseName)
    {
        var usedNames = new HashSet<string>(existingParameterNames, StringComparer.Ordinal);
        if (!usedNames.Contains(baseName))
            return baseName;

        for (var i = 1; ; i++)
        {
            var candidate = baseName + i.ToString(CultureInfo.InvariantCulture);
            if (!usedNames.Contains(candidate))
                return candidate;
        }
    }

    private static bool TryGetAnonymousFunctionOperation(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken, [NotNullWhen(true)] out IAnonymousFunctionOperation? lambdaOperation)
    {
        var operation = semanticModel.GetOperation(node, cancellationToken);
        if (TryGetAnonymousFunctionOperation(operation, out lambdaOperation))
            return true;

        foreach (var ancestor in node.AncestorsAndSelf())
        {
            operation = semanticModel.GetOperation(ancestor, cancellationToken);
            if (TryGetAnonymousFunctionOperation(operation, out lambdaOperation))
                return true;
        }

        lambdaOperation = null;
        return false;
    }

    private static bool TryGetAnonymousFunctionOperation(IOperation? operation, [NotNullWhen(true)] out IAnonymousFunctionOperation? lambdaOperation)
    {
        if (operation is null)
        {
            lambdaOperation = null;
            return false;
        }

        operation = operation.UnwrapConversionOperations();

        if (operation is IAnonymousFunctionOperation anonymousFunctionOperation)
        {
            lambdaOperation = anonymousFunctionOperation;
            return true;
        }

        if (operation is IDelegateCreationOperation { Target: IAnonymousFunctionOperation delegateTarget })
        {
            lambdaOperation = delegateTarget;
            return true;
        }

        lambdaOperation = null;
        return false;
    }

    private static bool TryGetInvocationAndArgument(IAnonymousFunctionOperation lambdaOperation, [NotNullWhen(true)] out IInvocationOperation? invocationOperation, [NotNullWhen(true)] out IArgumentOperation? lambdaArgument)
    {
        var parentOperation = lambdaOperation.Parent;
        if (parentOperation is IDelegateCreationOperation delegateCreationOperation)
        {
            parentOperation = delegateCreationOperation.Parent;
        }

        if (parentOperation is IArgumentOperation argumentOperation && argumentOperation.Parent is IInvocationOperation parentInvocation)
        {
            invocationOperation = parentInvocation;
            lambdaArgument = argumentOperation;
            return true;
        }

        invocationOperation = null;
        lambdaArgument = null;
        return false;
    }

    private static bool TryGetLocalOrParameterSymbol(IArgumentOperation argumentOperation, [NotNullWhen(true)] out ISymbol? symbol)
    {
        var operation = argumentOperation.Value.UnwrapConversionOperations();
        if (operation is ILocalReferenceOperation localReferenceOperation)
        {
            symbol = localReferenceOperation.Local;
            return true;
        }

        if (operation is IParameterReferenceOperation parameterReferenceOperation)
        {
            symbol = parameterReferenceOperation.Parameter;
            return true;
        }

        symbol = null;
        return false;
    }

    [return: NotNullIfNotNull(nameof(node))]
    private static SyntaxNode? GetDataFlowArgument(SyntaxNode? node)
    {
        if (node is ArrowExpressionClauseSyntax expression)
            return expression.Expression;

        return node;
    }

    private sealed class SymbolReferenceRewriter(SemanticModel semanticModel, ISymbol symbolToReplace, string replacementParameterName) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol is not null && symbol.IsEqualTo(symbolToReplace))
            {
                return IdentifierName(replacementParameterName).WithTriviaFrom(node);
            }

            return base.VisitIdentifierName(node);
        }
    }
}
