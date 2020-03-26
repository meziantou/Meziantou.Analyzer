using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AvoidComparisonWithBoolConstantAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.AvoidComparisonWithBoolConstant,
            title: "Avoid comparison with bool contant",
            messageFormat: "Avoid comparison with bool constant",
            RuleCategories.Style,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidComparisonWithBoolConstant));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(AnalyzeBinaryOperation, OperationKind.Binary);
        }

        private static void AnalyzeBinaryOperation(OperationAnalysisContext context)
        {
            var binaryOperation = (IBinaryOperation)context.Operation;

            if (binaryOperation.OperatorKind != BinaryOperatorKind.Equals &&
                binaryOperation.OperatorKind != BinaryOperatorKind.NotEquals)
            {
                return;
            }

            // There must be 2 valid operands
            if (binaryOperation.LeftOperand is null || binaryOperation.RightOperand is null)
                return;

            // Operands must be explicit
            if (binaryOperation.LeftOperand.IsImplicit || binaryOperation.RightOperand.IsImplicit)
                return;

            if (IsConstantBool(binaryOperation.LeftOperand) || IsConstantBool(binaryOperation.RightOperand))
            {
                var operatorTokenLocation = ((BinaryExpressionSyntax)binaryOperation.Syntax).OperatorToken.GetLocation();
                var diagnostic = Diagnostic.Create(s_rule, operatorTokenLocation);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsConstantBool(IOperation operation)
        {
            return operation.Type.IsBoolean() && operation.ConstantValue.HasValue;
        }
    }
}
