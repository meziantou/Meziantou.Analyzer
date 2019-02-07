using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.UsageRules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseStringEqualsAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseStringEquals,
            title: "use String.Equals",
            messageFormat: "Use string.Equals instead of {0}",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringEquals));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.BinaryOperator);
        }

        private void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (operation.OperatorKind == BinaryOperatorKind.Equals ||
                operation.OperatorKind == BinaryOperatorKind.NotEquals)
            {
                if (operation.LeftOperand.Type.IsString() && operation.RightOperand.Type.IsString())
                {
                    if (IsNull(operation.LeftOperand) || IsNull(operation.RightOperand))
                        return;

                    context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), $"{operation.OperatorKind} operator"));
                }
            }
        }

        private static bool IsNull(IOperation operation)
        {
            return operation.ConstantValue.HasValue && operation.ConstantValue.Value == null;
        }

    }
}
