namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AwaitTaskBeforeDisposingResourcesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.AwaitTaskBeforeDisposingResources,
        title: "Await task before disposing of resources",
        messageFormat: "Await task before disposing of resources",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Await the task before the end of the enclosing using block.",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AwaitTaskBeforeDisposingResources));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeReturn, OperationKind.Return);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly AwaitableTypes _awaitableTypes = new(compilation);

        public INamedTypeSymbol? AsyncFlowControlSymbol { get; set; } = compilation.GetBestTypeByMetadataName("System.Threading.AsyncFlowControl");

        public void AnalyzeReturn(OperationAnalysisContext context)
        {
            var op = (IReturnOperation)context.Operation;
            var returnedValue = op.ReturnedValue;
            if (returnedValue is null)
                return;

            var returnType = returnedValue.UnwrapImplicitConversions().Type;
            if (!_awaitableTypes.IsAwaitable(returnType, returnedValue.SemanticModel!, returnedValue.Syntax.GetLocation().SourceSpan.End))
                return;

            // Must be in a using block
            if (!IsInUsingOperation(op))
                return;

            if (!NeedAwait(returnedValue))
                return;

            context.ReportDiagnostic(Rule, op);
        }

        /// <summary>
        /// Checks if the operation is within a using block that requires awaiting tasks.
        /// Returns false if the disposable is AsyncFlowControl (from ExecutionContext.SuppressFlow()),
        /// as it's safe to return tasks without awaiting in that case.
        /// </summary>
        private bool IsInUsingOperation(IOperation operation)
        {
            foreach (var parent in operation.Ancestors().Select(operation => operation.UnwrapLabels()))
            {
                if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
                    return false;

                if (parent is IUsingOperation usingOp)
                {
                    // Exception: ExecutionContext.SuppressFlow() returns AsyncFlowControl
                    // The task doesn't need to be awaited before the using block ends
                    if (IsAsyncFlowControl(usingOp.Resources))
                        return false;

                    return true;
                }

                if (parent is IBlockOperation block)
                {
                    foreach (var blockOperation in block.Operations.Select(operation => operation.UnwrapLabels()))
                    {
                        if (blockOperation == operation)
                            break;

                        if (blockOperation is IUsingDeclarationOperation usingDecl)
                        {
                            // Exception: ExecutionContext.SuppressFlow() returns AsyncFlowControl
                            // The task doesn't need to be awaited before the using block ends
                            if (IsAsyncFlowControl(usingDecl.DeclarationGroup))
                                return false;

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the operation is an AsyncFlowControl (from ExecutionContext.SuppressFlow()).
        /// AsyncFlowControl is exempt from MA0100 because the execution context is captured at task creation time,
        /// making it safe to return tasks without awaiting before the using block ends.
        /// </summary>
        private bool IsAsyncFlowControl(IOperation? operation)
        {
            if (operation is null || AsyncFlowControlSymbol is null)
                return false;

            // For using declarations (using var x = ...), we need to drill down through the declaration structure
            if (operation is IVariableDeclarationGroupOperation variableDeclarationGroupOperation)
            {
                return variableDeclarationGroupOperation.Declarations
                    .SelectMany(d => d.Declarators)
                    .Any(declarator => declarator.Initializer?.Value?.Type?.IsEqualTo(AsyncFlowControlSymbol) == true);
            }

            var type = operation.Type;
            if (type is null)
                return false;

            return type.IsEqualTo(AsyncFlowControlSymbol);
        }

        private bool NeedAwait(IOperation operation)
        {
            while (operation is IConversionOperation conversion)
            {
                operation = conversion.Operand;
            }

            if (operation is null)
                return false;

            if (operation.Kind == OperationKind.DefaultValue)
                return false;

            // (Task)null
            if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is null)
                return false;

            return !_awaitableTypes.IsCompletedTask(operation);
        }
    }
}
