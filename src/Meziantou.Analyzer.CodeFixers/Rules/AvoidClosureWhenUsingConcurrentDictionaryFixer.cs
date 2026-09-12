using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class AvoidClosureWhenUsingConcurrentDictionaryFixer : CodeFixProvider
{
    private static readonly SymbolDisplayFormat TypeDisplayFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

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
            var updatedLambda = CreateLambdaUsingParameters(semanticModel, invocationOperation, lambdaOperation, lambdaArgument);
            if (updatedLambda is not null)
            {
                var title = "Use lambda parameters";
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title,
                        ct => ReplaceNode(context.Document, lambdaOperation.Syntax, updatedLambda, ct),
                        equivalenceKey: title),
                    context.Diagnostics);
            }
        }

        if (context.Diagnostics.Any(d => d.Id == RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionaryByUsingFactoryArg))
        {
            var newInvocation = CreateInvocationWithFactoryArg(semanticModel, invocationOperation, lambdaOperation);
            if (newInvocation is not null)
            {
                var title = "Use factoryArgument overload";
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title,
                        ct => ReplaceNode(context.Document, invocationOperation.Syntax, newInvocation, ct),
                        equivalenceKey: title),
                    context.Diagnostics);
            }
        }
    }

    private static async Task<Document> ReplaceNode(Document document, SyntaxNode nodeToReplace, SyntaxNode newNode, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(nodeToReplace, newNode.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static AnonymousFunctionExpressionSyntax? CreateLambdaUsingParameters(SemanticModel semanticModel, IInvocationOperation invocationOperation, IAnonymousFunctionOperation lambdaOperation, IArgumentOperation lambdaArgument)
    {
        var mappings = GetReplacementMappings(invocationOperation, lambdaOperation, lambdaArgument);
        if (mappings.Count == 0)
            return null;

        var updatedLambda = (AnonymousFunctionExpressionSyntax)lambdaOperation.Syntax;
        foreach (var (symbol, parameterName) in mappings)
        {
            updatedLambda = ReplaceSymbolReferences(updatedLambda, semanticModel, symbol, parameterName);
        }

        return updatedLambda;
    }

    private static InvocationExpressionSyntax? CreateInvocationWithFactoryArg(SemanticModel semanticModel, IInvocationOperation invocationOperation, IAnonymousFunctionOperation lambdaOperation)
    {
        if (invocationOperation.Syntax is not InvocationExpressionSyntax invocationSyntax)
            return null;

        if (GetCapturedSymbolToReplace(semanticModel, lambdaOperation) is not { } captured)
            return null;

        var (capturedSymbol, capturedSymbolType) = captured;

        return invocationOperation.TargetMethod.Name switch
        {
            "GetOrAdd" => CreateGetOrAddInvocationWithFactoryArg(invocationOperation, invocationSyntax, lambdaOperation, capturedSymbol, capturedSymbolType, semanticModel),
            "AddOrUpdate" => CreateAddOrUpdateInvocationWithFactoryArg(invocationOperation, invocationSyntax, capturedSymbol, capturedSymbolType, semanticModel),
            _ => null,
        };
    }

    private static (ISymbol Symbol, ITypeSymbol Type)? GetCapturedSymbolToReplace(SemanticModel semanticModel, IAnonymousFunctionOperation lambdaOperation)
    {
        return GetCapturedSymbols(semanticModel, lambdaOperation).FirstOrDefault() switch
        {
            ILocalSymbol local => (local, local.Type),
            IParameterSymbol parameter => (parameter, parameter.Type),
            _ => null,
        };
    }

    private static InvocationExpressionSyntax? CreateGetOrAddInvocationWithFactoryArg(IInvocationOperation invocationOperation, InvocationExpressionSyntax invocationSyntax, IAnonymousFunctionOperation lambdaOperation, ISymbol capturedSymbol, ITypeSymbol capturedSymbolType, SemanticModel semanticModel)
    {
        if (invocationOperation.Arguments.Length != 2)
            return null;

        if (lambdaOperation.Syntax is not AnonymousFunctionExpressionSyntax lambda)
            return null;

        var parameterName = GetUniqueParameterName(lambdaOperation.Symbol.Parameters.Select(p => p.Name), "arg");
        var parameter = CreateFactoryArgumentParameter(lambda, parameterName, capturedSymbolType, semanticModel);
        if (parameter is null)
            return null;

        var updatedLambda = ReplaceSymbolReferences(lambda, semanticModel, capturedSymbol, parameterName);
        var lambdaWithParameter = AddParameterToLambda(updatedLambda, parameter);
        if (lambdaWithParameter is null)
            return null;

        var arguments = invocationSyntax.ArgumentList.Arguments;
        arguments = arguments.Replace(arguments[1], arguments[1].WithExpression(lambdaWithParameter));
        arguments = arguments.Add(Argument(IdentifierName(capturedSymbol.Name)));
        return invocationSyntax.WithArgumentList(invocationSyntax.ArgumentList.WithArguments(arguments));
    }

    private static InvocationExpressionSyntax? CreateAddOrUpdateInvocationWithFactoryArg(IInvocationOperation invocationOperation, InvocationExpressionSyntax invocationSyntax, ISymbol capturedSymbol, ITypeSymbol capturedSymbolType, SemanticModel semanticModel)
    {
        if (invocationOperation.Arguments.Length != 3)
            return null;

        if (!TryGetAnonymousFunctionOperation(invocationOperation.Arguments[1].Value, out var addValueFactoryOperation))
            return null;

        if (!TryGetAnonymousFunctionOperation(invocationOperation.Arguments[2].Value, out var updateValueFactoryOperation))
            return null;

        if (addValueFactoryOperation.Syntax is not AnonymousFunctionExpressionSyntax addValueFactory)
            return null;

        if (updateValueFactoryOperation.Syntax is not AnonymousFunctionExpressionSyntax updateValueFactory)
            return null;

        var parameterName = GetUniqueParameterName(
            addValueFactoryOperation.Symbol.Parameters.Select(p => p.Name).Concat(updateValueFactoryOperation.Symbol.Parameters.Select(p => p.Name)),
            "arg");

        // Both lambdas get their own parameter, as one can be explicitly typed while the other is not
        var addValueFactoryParameter = CreateFactoryArgumentParameter(addValueFactory, parameterName, capturedSymbolType, semanticModel);
        var updateValueFactoryParameter = CreateFactoryArgumentParameter(updateValueFactory, parameterName, capturedSymbolType, semanticModel);
        if (addValueFactoryParameter is null || updateValueFactoryParameter is null)
            return null;

        var newAddValueFactory = AddParameterToLambda(ReplaceSymbolReferences(addValueFactory, semanticModel, capturedSymbol, parameterName), addValueFactoryParameter);
        if (newAddValueFactory is null)
            return null;

        var newUpdateValueFactory = AddParameterToLambda(ReplaceSymbolReferences(updateValueFactory, semanticModel, capturedSymbol, parameterName), updateValueFactoryParameter);
        if (newUpdateValueFactory is null)
            return null;

        var arguments = invocationSyntax.ArgumentList.Arguments;
        arguments = arguments.Replace(arguments[1], arguments[1].WithExpression(newAddValueFactory));
        arguments = arguments.Replace(arguments[2], arguments[2].WithExpression(newUpdateValueFactory));
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

    /// <summary>
    /// Creates the parameter receiving the <c>factoryArgument</c> in <paramref name="lambda"/>. The parameter must be
    /// explicitly typed when the existing parameters are, as C# requires the parameters of a lambda to be either all
    /// explicitly typed or all implicitly typed (CS0748).
    /// </summary>
    private static ParameterSyntax? CreateFactoryArgumentParameter(AnonymousFunctionExpressionSyntax lambda, string parameterName, ITypeSymbol parameterType, SemanticModel semanticModel)
    {
        var parameter = Parameter(Identifier(parameterName));
        if (lambda is not ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
            return parameter; // The single parameter of a simple lambda is always implicitly typed

        var parameters = parenthesizedLambda.ParameterList.Parameters;

        // No parameter can be added after a parameter with a default value or a 'params' parameter
        if (parameters.Any(p => p.Default is not null || p.Modifiers.Any(SyntaxKind.ParamsKeyword)))
            return null;

        if (!parameters.Any(p => p.Type is not null))
            return parameter;

        var typeSyntax = CreateTypeSyntax(parameterType, semanticModel, lambda.SpanStart);
        if (typeSyntax is null)
            return null;

        return parameter.WithType(typeSyntax);
    }

    private static TypeSyntax? CreateTypeSyntax(ITypeSymbol type, SemanticModel semanticModel, int position)
    {
        if (type.TypeKind is TypeKind.Error || type.IsAnonymousType)
            return null;

        // The type may still not be expressible, such as when it contains an anonymous type
        var typeSyntax = ParseTypeName(type.ToMinimalDisplayString(semanticModel, position, TypeDisplayFormat), options: semanticModel.SyntaxTree.Options);
        return typeSyntax.ContainsDiagnostics ? null : typeSyntax;
    }

    private static ParenthesizedLambdaExpressionSyntax? AddParameterToLambda(AnonymousFunctionExpressionSyntax lambda, ParameterSyntax parameter)
    {
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

        operation = operation.UnwrapConversions();

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
        var operation = argumentOperation.Value.UnwrapConversions();
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
