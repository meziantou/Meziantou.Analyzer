using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseZeroToInitializeAnEnumValue : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotUseZeroToInitializeAnEnumValue,
            title: "Use Explicit enum value instead of 0",
            messageFormat: "Use Explicit enum value instead of 0",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseZeroToInitializeAnEnumValue));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Conversion);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IConversionOperation)context.Operation;
            if (!operation.IsImplicit)
                return;

            if (!operation.Type.IsEnumeration())
                return;

            if (operation.Operand.ConstantValue.Value is int i && i == 0)
            {
                context.ReportDiagnostic(s_rule, operation);
            }
        }
    }
}
