using System.Collections.Immutable;
using System.Threading.Tasks;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AwaitTaskBeforeDisposingResourcesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.AwaitTaskBeforeDisposingResources,
        title: "Await task before disposing of resources",
        messageFormat: "Await task before disposing of resources",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Await the task before the end of the enclosing using block.",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AwaitTaskBeforeDisposingResources));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeReturn, OperationKind.Return);
        });
    }

    private sealed class AnalyzerContext
    {
        private readonly AwaitableTypes _awaitableTypes;

        public AnalyzerContext(Compilation compilation)
        {
            _awaitableTypes = new AwaitableTypes(compilation);

            TaskSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
            TaskOfTSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");
            ValueTaskSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask");
            ValueTaskOfTSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        }

        public INamedTypeSymbol? TaskSymbol { get; set; }
        public INamedTypeSymbol? TaskOfTSymbol { get; set; }
        public INamedTypeSymbol? ValueTaskSymbol { get; set; }
        public INamedTypeSymbol? ValueTaskOfTSymbol { get; set; }

        public void AnalyzeReturn(OperationAnalysisContext context)
        {
            var op = (IReturnOperation)context.Operation;
            var returnedValue = op.ReturnedValue;
            if (returnedValue is null)
                return;

            var returnType = returnedValue.UnwrapImplicitConversionOperations().Type;
            if (!_awaitableTypes.IsAwaitable(returnType, returnedValue.SemanticModel!, returnedValue.Syntax.GetLocation().SourceSpan.End))
                return;

            // Must be in a using block
            if (!IsInUsingOperation(op))
                return;

            if (!NeedAwait(returnedValue))
                return;

            context.ReportDiagnostic(s_rule, op);
        }

        private static bool IsInUsingOperation(IOperation operation)
        {
            foreach (var parent in operation.Ancestors())
            {
                if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
                    return false;

                if (parent is IUsingOperation)
                    return true;
            }

            return false;
        }

        private bool NeedAwait(IOperation operation)
        {
            while (operation is IConversionOperation conversion)
            {
                operation = conversion.Operand;
            }

            if (operation == null)
                return false;

            if (operation.Kind == OperationKind.DefaultValue)
                return false;

            // (Task)null
            if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is null)
                return false;

            // Task.CompletedTask
            if (operation.Kind == OperationKind.PropertyReference)
            {
                var prop = (IPropertyReferenceOperation)operation;
                if (prop.Property.Name == nameof(Task.CompletedTask) && prop.Property.ContainingType.IsEqualToAny(TaskSymbol, ValueTaskSymbol))
                    return false;
            }

            // Task.FromResult, Task.FromCanceled, FromException
            if (operation.Kind == OperationKind.Invocation)
            {
                var invocation = (IInvocationOperation)operation;
                if (invocation.TargetMethod.Name is nameof(Task.FromResult) or nameof(Task.FromCanceled) or nameof(Task.FromException) &&
                    invocation.TargetMethod.ContainingType.IsEqualToAny(TaskSymbol, ValueTaskSymbol))
                    return false;
            }

            if (operation.Kind == OperationKind.ObjectCreation)
            {
                var create = (IObjectCreationOperation)operation;
                if (create.Type is not null)
                {
                    // new ValueTask()
                    if (create.Type.OriginalDefinition.IsEqualTo(ValueTaskSymbol) &&
                        create.Arguments.Length == 0)
                        return false;

                    // new ValueTask<T>(T value)
                    if (create.Type.OriginalDefinition.IsEqualTo(ValueTaskOfTSymbol) &&
                        create.Arguments.Length == 1 &&
                        create.Arguments[0].Parameter is { } firstParameter &&
                        firstParameter.Type.IsEqualTo(((INamedTypeSymbol?)create.Type)?.TypeArguments[0]))
                        return false;
                }
            }

            return true;
        }
    }
}
