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
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use lambda parameters",
                    ct => UseLambdaParameters(context.Document, semanticModel, invocationOperation, lambdaOperation, lambdaArgument, ct),
                    equivalenceKey: "Use lambda parameters"),
                context.Diagnostics);
        }

        if (context.Diagnostics.Any(d => d.Id == RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionaryByUsingFactoryArg)
            && invocationOperation.Syntax is InvocationExpressionSyntax invocationSyntax
            && GetFactoriesToUpdate(invocationOperation) is { } factories
            && TryGetFactoryArgumentSymbol(semanticModel, factories, lambdaOperation, out var capturedSymbol)
            && CreateFactoryArgumentParameters(semanticModel, factories, capturedSymbol) is { } parameters)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use factoryArgument overload",
                    ct => UseFactoryArgumentOverload(context.Document, semanticModel, invocationSyntax, factories, parameters, capturedSymbol, ct),
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

    private static async Task<Document> UseFactoryArgumentOverload(Document document, SemanticModel semanticModel, InvocationExpressionSyntax invocationSyntax, List<IAnonymousFunctionOperation> factories, List<ParameterSyntax> parameters, ISymbol capturedSymbol, CancellationToken cancellationToken)
    {
        var arguments = invocationSyntax.ArgumentList.Arguments;
        for (var i = 0; i < factories.Count; i++)
        {
            var parameter = parameters[i];
            var updatedLambda = ReplaceSymbolReferences((AnonymousFunctionExpressionSyntax)factories[i].Syntax, semanticModel, capturedSymbol, parameter.Identifier.ValueText);
            var lambdaWithParameter = AddParameterToLambda(updatedLambda, parameter);

            // The factories are the arguments following the key
            var argumentIndex = i + 1;
            arguments = arguments.Replace(arguments[argumentIndex], arguments[argumentIndex].WithExpression(lambdaWithParameter));
        }

        arguments = arguments.Add(Argument(IdentifierName(capturedSymbol.Name)));
        var newInvocation = invocationSyntax.WithArgumentList(invocationSyntax.ArgumentList.WithArguments(arguments));

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(invocationSyntax, newInvocation.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    /// <summary>
    /// Gets the factories the code fix must rewrite to use the 'factoryArgument' parameter, in the order of the arguments following the key.
    /// </summary>
    private static List<IAnonymousFunctionOperation>? GetFactoriesToUpdate(IInvocationOperation invocationOperation)
    {
        if (invocationOperation.TargetMethod.Name is "GetOrAdd" && invocationOperation.Arguments.Length == 2)
        {
            if (TryGetAnonymousFunctionOperation(invocationOperation.Arguments[1].Value, out var valueFactory))
                return [valueFactory];
        }
        else if (invocationOperation.TargetMethod.Name is "AddOrUpdate" && invocationOperation.Arguments.Length == 3)
        {
            if (TryGetAnonymousFunctionOperation(invocationOperation.Arguments[1].Value, out var addValueFactory) &&
                TryGetAnonymousFunctionOperation(invocationOperation.Arguments[2].Value, out var updateValueFactory))
            {
                return [addValueFactory, updateValueFactory];
            }
        }

        return null;
    }

    private static bool TryGetFactoryArgumentSymbol(SemanticModel semanticModel, List<IAnonymousFunctionOperation> factories, IAnonymousFunctionOperation lambdaOperation, [NotNullWhen(true)] out ISymbol? capturedSymbol)
    {
        capturedSymbol = null;

        // The code fix adds a parameter to every rewritten factory
        if (factories.Any(factory => factory.Syntax is not (ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax)))
            return false;

        var symbol = GetCapturedSymbols(semanticModel, lambdaOperation).FirstOrDefault();
        if (symbol is not ILocalSymbol and not IParameterSymbol)
            return false;

        // The captured variable is a shared storage, whereas the 'factoryArgument' parameter is a copy of its value.
        // Replacing the references to the variable by the parameter would drop the writes made by the factories.
        if (factories.Any(factory => IsWrittenInside(semanticModel, factory, symbol)))
            return false;

        capturedSymbol = symbol;
        return true;
    }

    /// <summary>
    /// Creates the parameter receiving the <c>factoryArgument</c> in each factory, or <see langword="null"/> when one
    /// of the factories cannot get it.
    /// </summary>
    private static List<ParameterSyntax>? CreateFactoryArgumentParameters(SemanticModel semanticModel, List<IAnonymousFunctionOperation> factories, ISymbol capturedSymbol)
    {
        var parameterType = capturedSymbol switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            _ => null,
        };

        if (parameterType is null)
            return null;

        var parameterName = GetUniqueParameterName(factories.SelectMany(factory => factory.Symbol.Parameters).Select(p => p.Name), "arg");

        var parameters = new List<ParameterSyntax>(factories.Count);
        foreach (var factory in factories)
        {
            // Each factory gets its own parameter, as one can be explicitly typed while another is not
            var parameter = CreateFactoryArgumentParameter((AnonymousFunctionExpressionSyntax)factory.Syntax, parameterName, parameterType, semanticModel);
            if (parameter is null)
                return null;

            parameters.Add(parameter);
        }

        return parameters;
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
            if (!parameters.Contains(symbol, SymbolEqualityComparer.Default) && !dataFlow.WrittenInside.Contains(symbol, SymbolEqualityComparer.Default))
            {
                yield return symbol;
            }
        }
    }

    private static bool IsWrittenInside(SemanticModel semanticModel, IAnonymousFunctionOperation lambdaOperation, ISymbol symbol)
    {
        var dataFlowNode = GetDataFlowArgument(lambdaOperation.Syntax);
        if (dataFlowNode is null)
            return false;

        var dataFlow = semanticModel.AnalyzeDataFlow(dataFlowNode);
        return dataFlow.WrittenInside.Contains(symbol, SymbolEqualityComparer.Default);
    }

    private static AnonymousFunctionExpressionSyntax ReplaceSymbolReferences(AnonymousFunctionExpressionSyntax lambda, SemanticModel semanticModel, ISymbol symbolToReplace, string replacementParameterName)
    {
        var rewriter = new SymbolReferenceRewriter(semanticModel, symbolToReplace, replacementParameterName);
        return (AnonymousFunctionExpressionSyntax)rewriter.Visit(lambda);
    }

    private static ParenthesizedLambdaExpressionSyntax AddParameterToLambda(AnonymousFunctionExpressionSyntax lambda, ParameterSyntax parameter)
    {
        if (lambda is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
        {
            return parenthesizedLambda.WithParameterList(parenthesizedLambda.ParameterList.WithParameters(parenthesizedLambda.ParameterList.Parameters.Add(parameter)));
        }

        var simpleLambda = (SimpleLambdaExpressionSyntax)lambda;
        var parameters = SeparatedList(new[] { simpleLambda.Parameter, parameter });
        var updatedLambda = simpleLambda.Block is not null
            ? ParenthesizedLambdaExpression(ParameterList(parameters), simpleLambda.Block)
            : ParenthesizedLambdaExpression(ParameterList(parameters), simpleLambda.ExpressionBody!);

        return updatedLambda.WithAsyncKeyword(simpleLambda.AsyncKeyword);
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
