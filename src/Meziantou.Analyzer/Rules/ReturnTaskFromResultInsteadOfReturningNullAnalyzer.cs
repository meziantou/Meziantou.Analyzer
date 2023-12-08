using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReturnTaskFromResultInsteadOfReturningNullAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ReturnTaskFromResultInsteadOfReturningNull,
        title: "Return Task.FromResult instead of returning null",
        messageFormat: "Return Task.FromResult instead of returning null",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ReturnTaskFromResultInsteadOfReturningNull));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);
            if (analyzerContext.TaskOfTSymbol is not null || analyzerContext.TaskSymbol is not null)
            {
                context.RegisterOperationAction(analyzerContext.AnalyzeReturnOperation, OperationKind.Return);
            }
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        public INamedTypeSymbol? TaskSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
        public INamedTypeSymbol? TaskOfTSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");

        public void AnalyzeReturnOperation(OperationAnalysisContext context)
        {
            var operation = (IReturnOperation)context.Operation;
            if (!IsTaskType(operation.ReturnedValue?.Type))
                return;

            if (!MayBeNullValue(operation))
                return;

            // Find the owning symbol and check if it returns a task and doesn't use the async keyword
            var methodSymbol = ReturnTaskFromResultInsteadOfReturningNullAnalyzerCommon.FindContainingMethod(operation, context.CancellationToken);
            if (methodSymbol is null || !IsTaskType(methodSymbol.ReturnType))
                return;

            context.ReportDiagnostic(Rule, operation);
        }

        private bool MayBeNullValue([NotNullWhen(true)] IOperation? operation)
        {
            if (operation is null)
                return false;

            if (operation is IReturnOperation returnOperation)
            {
                operation = returnOperation.ReturnedValue;
                if (operation is null)
                    return false;
            }

            if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is null)
                return true;

            if (operation is IConversionOperation conversion)
            {
                if (!IsTaskType(conversion.Type))
                    return false;

                return MayBeNullValue(conversion.Operand);
            }

            if (operation is IConditionalAccessOperation conditionalAccess)
            {
                return MayBeNullValue(conditionalAccess.Operation) || MayBeNullValue(conditionalAccess.WhenNotNull);
            }
            else if (operation is IConditionalOperation conditional)
            {
                return MayBeNullValue(conditional.WhenTrue) || MayBeNullValue(conditional.WhenFalse);
            }
            else if (operation is ISwitchExpressionOperation switchExpression)
            {
                foreach (var arm in switchExpression.Arms)
                {
                    if (MayBeNullValue(arm.Value))
                        return true;
                }
            }

            return false;
        }

        private bool IsTaskType(ITypeSymbol? symbol)
        {
            if (symbol is null)
                return false;

            return symbol.IsEqualTo(TaskSymbol) || symbol.OriginalDefinition.IsEqualTo(TaskOfTSymbol);
        }
    }
}
