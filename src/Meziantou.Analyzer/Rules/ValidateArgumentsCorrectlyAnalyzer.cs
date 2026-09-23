namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValidateArgumentsCorrectlyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ValidateArgumentsCorrectly,
        title: "Validate arguments correctly in iterator methods",
        messageFormat: "Validate arguments correctly in iterator methods",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ValidateArgumentsCorrectly));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var compilation = ctx.Compilation;
            var analyzerContext = new AnalyzerContext(compilation);

            ctx.RegisterOperationAction(analyzerContext.AnalyzeMethodBody, OperationKind.MethodBody);
        });
    }

    private sealed class AnalyzerContext
    {
        private readonly HashSet<ISymbol> _symbols;
        private readonly INamedTypeSymbol? _argumentExceptionSymbol;

        public AnalyzerContext(Compilation compilation)
        {
            var symbols = new List<ISymbol>();
            symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.IEnumerable"));
            symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1"));
            symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1"));
            symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.IEnumerator"));
            symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerator`1"));
            symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1"));
            _symbols = new HashSet<ISymbol>(symbols, SymbolEqualityComparer.Default);

            _argumentExceptionSymbol = compilation.GetTypeByMetadataName("System.ArgumentException");
        }

        public bool CanContainsYield(IMethodSymbol methodSymbol)
        {
            // The code fixer only supports the methods (not the accessors, operators, ...)
            if (methodSymbol.MethodKind is not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation))
                return false;

            if (!_symbols.Contains(methodSymbol.ReturnType.OriginalDefinition))
                return false;

            return methodSymbol.Parameters.All(p => p.RefKind == RefKind.None);
        }

        internal void AnalyzeMethodBody(OperationAnalysisContext context)
        {
            var operation = (IMethodBodyOperation)context.Operation;
            if (operation.BlockBody is null || context.ContainingSymbol is not IMethodSymbol methodSymbol || !CanContainsYield(methodSymbol))
                return;

            var state = new AnalysisState();
            foreach (var statement in operation.BlockBody.Operations)
            {
                state.CurrentStatementEnd = statement.Syntax.Span.End;
                Visit(statement, state);
            }

            if (state.FirstYieldStart is null || state.LastValidationEnd is null)
                return;

            if (state.LastValidationEnd < state.FirstYieldStart)
            {
                // The validation cannot be done eagerly when it comes after an await, as the method that validates the arguments is not async
                if (state.FirstAwaitStart < state.LastValidationEnd)
                    return;

                var properties = ImmutableDictionary.Create<string, string?>(StringComparer.Ordinal)
                    .Add(ValidateArgumentsCorrectlyAnalyzerCommon.IndexKey, state.LastValidationEnd.Value.ToString(CultureInfo.InvariantCulture));

                context.ReportDiagnostic(Rule, properties, methodSymbol);
            }
        }

        private void Visit(IOperation operation, AnalysisState state)
        {
            // The nested functions are not executed by the method
            if (operation is IAnonymousFunctionOperation or ILocalFunctionOperation)
                return;

            var start = operation.Syntax.SpanStart;
            if (operation is IReturnOperation { Kind: OperationKind.YieldReturn or OperationKind.YieldBreak })
            {
                state.FirstYieldStart = Min(state.FirstYieldStart, start);
            }
            else if (IsAwait(operation))
            {
                state.FirstAwaitStart = Min(state.FirstAwaitStart, start);
            }
            else if (IsArgumentValidation(operation))
            {
                // The validation ends at the end of the top-level statement that contains it
                state.LastValidationEnd = state.CurrentStatementEnd;
            }

            foreach (var child in operation.GetChildOperations())
            {
                Visit(child, state);
            }

            static int Min(int? current, int value) => current is null || value < current ? value : current.Value;
        }

        private static bool IsAwait(IOperation operation)
        {
            return operation switch
            {
                IAwaitOperation => true,
                IForEachLoopOperation forEachLoop => forEachLoop.IsAsynchronous,
                IUsingOperation usingOperation => usingOperation.IsAsynchronous,
                IUsingDeclarationOperation usingDeclaration => usingDeclaration.IsAsynchronous,
                _ => false,
            };
        }

        private bool IsArgumentValidation(IOperation operation)
        {
            return operation switch
            {
                IThrowOperation { Exception: not null } throwOperation => throwOperation.Exception.UnwrapImplicitConversions().Type is { } type && type.IsOrInheritsFrom(_argumentExceptionSymbol),
                IInvocationOperation { TargetMethod: var targetMethod } => targetMethod.IsStatic &&
                    targetMethod.ContainingType.IsOrInheritsFrom(_argumentExceptionSymbol) &&
                    targetMethod.Name.StartsWith("Throw", StringComparison.Ordinal),
                _ => false,
            };
        }

        private sealed class AnalysisState
        {
            public int CurrentStatementEnd { get; set; }
            public int? FirstYieldStart { get; set; }
            public int? FirstAwaitStart { get; set; }
            public int? LastValidationEnd { get; set; }
        }
    }
}
