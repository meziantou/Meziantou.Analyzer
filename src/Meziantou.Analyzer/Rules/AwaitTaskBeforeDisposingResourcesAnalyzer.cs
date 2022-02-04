using System.Collections.Immutable;
using System.Threading.Tasks;
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

        context.RegisterOperationAction(AnalyzeReturn, OperationKind.Return);
    }

    private void AnalyzeReturn(OperationAnalysisContext context)
    {
        var op = (IReturnOperation)context.Operation;
        var returnedValue = op.ReturnedValue;
        if (returnedValue is null)
            return;

        var taskSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfTSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var valueTaskOfTSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        if (IsTaskLike(returnedValue.Type))
        {
            // Must be in a using block
            if (!IsInUsingOperation(op))
                return;

            if (!NeedAwait(returnedValue))
                return;

            context.ReportDiagnostic(s_rule, op);
        }

        static bool IsInUsingOperation(IOperation operation)
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

        bool IsTaskLike(ITypeSymbol? symbol)
        {
            return symbol != null && symbol.OriginalDefinition.IsEqualToAny(taskSymbol, taskOfTSymbol, valueTaskSymbol, valueTaskOfTSymbol);
        }

        bool NeedAwait(IOperation operation)
        {
            while (operation is IConversionOperation conversion)
            {
                operation = conversion.Operand;
            }

            if (operation == null)
                return false;

            // default(Task)
            if (operation is IDefaultValueOperation)
                return false;

            // (Task)null
            if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is null)
                return false;

            // Task.CompletedTask
            if (operation is IPropertyReferenceOperation prop &&
                prop.Property.Name == nameof(Task.CompletedTask) &&
                prop.Property.ContainingType.IsEqualToAny(taskSymbol, valueTaskSymbol))
                return false;

            // Task.FromResult, Task.FromCanceled, FromException
            if (operation is IInvocationOperation invocation &&
                (invocation.TargetMethod.Name is nameof(Task.FromResult) or nameof(Task.FromCanceled) or nameof(Task.FromException)) &&
                invocation.TargetMethod.ContainingType.IsEqualToAny(taskSymbol, valueTaskSymbol))
                return false;

            // new ValueTask()
            if (operation is IObjectCreationOperation create &&
                create.Type != null &&
                create.Type.OriginalDefinition.IsEqualTo(valueTaskSymbol) &&
                create.Arguments.Length == 0)
                return false;

            // new ValueTask<T>(T value)
            if (operation is IObjectCreationOperation create2 &&
                create2.Type != null &&
                create2.Type.OriginalDefinition.IsEqualTo(valueTaskOfTSymbol) &&
                create2.Arguments.Length == 1 &&
                create2.Arguments[0].Parameter is { } firstParameter &&
                firstParameter.Type.IsEqualTo(((INamedTypeSymbol?)create2.Type)?.TypeArguments[0]))
                return false;

            return true;
        }
    }
}
