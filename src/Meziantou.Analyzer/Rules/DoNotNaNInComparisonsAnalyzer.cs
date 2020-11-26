using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotNaNInComparisonsAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.DoNotNaNInComparisons,
            title: "NaN should not be used in comparisons",
            messageFormat: "{0}.NaN should not be used in comparisons",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotNaNInComparisons));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var analyzerContext = new AnalyzerContext(ctx.Compilation);
                ctx.RegisterOperationAction(analyzerContext.AnalyzeBinaryOperator, OperationKind.Binary);
            });
        }

        private sealed class AnalyzerContext
        {
            public ISymbol? DoubleNaN { get; }
            public ISymbol? SingleNaN { get; }
            public ISymbol? HalfNaN { get; }

            public AnalyzerContext(Compilation compilation)
            {
                DoubleNaN = compilation.GetTypeByMetadataName("System.Double")?.GetMembers("NaN").FirstOrDefault();
                SingleNaN = compilation.GetTypeByMetadataName("System.Single")?.GetMembers("NaN").FirstOrDefault();
                HalfNaN = compilation.GetTypeByMetadataName("System.Half")?.GetMembers("NaN").FirstOrDefault();
            }

            public void AnalyzeBinaryOperator(OperationAnalysisContext context)
            {
                var operation = (IBinaryOperation)context.Operation;
                if (operation.OperatorKind == BinaryOperatorKind.Equals || operation.OperatorKind == BinaryOperatorKind.NotEquals)
                {
                    AnalyzeOperand(context, operation.LeftOperand);
                    AnalyzeOperand(context, operation.RightOperand);
                }
            }

            private void AnalyzeOperand(OperationAnalysisContext context, IOperation operation)
            {
                while (operation is IConversionOperation conversion)
                {
                    operation = conversion.Operand;
                }

                if (operation is IMemberReferenceOperation memberReference)
                {
                    if (memberReference.Member.IsEqualTo(DoubleNaN))
                    {
                        context.ReportDiagnostic(s_rule, operation, "System.Double");
                    }
                    else if (memberReference.Member.IsEqualTo(SingleNaN))
                    {
                        context.ReportDiagnostic(s_rule, operation, "System.Single");
                    }
                    else if (memberReference.Member.IsEqualTo(HalfNaN))
                    {
                        context.ReportDiagnostic(s_rule, operation, "System.Half");
                    }
                }
            }
        }
    }
}
