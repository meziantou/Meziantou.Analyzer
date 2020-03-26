using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AvoidComparisonWithBoolLiteralAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.AvoidComparisonWithBoolLiteral,
            title: "Avoid comparison with bool literal",
            messageFormat: "Avoid comparison with bool literal",
            RuleCategories.Style,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidComparisonWithBoolLiteral));

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

            if (binaryOperation.LeftOperand is null || binaryOperation.RightOperand is null)
                return;

            if ((binaryOperation.LeftOperand.Type.IsBoolean() && binaryOperation.LeftOperand.Kind == OperationKind.Literal &&
                !binaryOperation.RightOperand.IsImplicit) ||
                (binaryOperation.RightOperand.Type.IsBoolean() && binaryOperation.RightOperand.Kind == OperationKind.Literal &&
                !binaryOperation.LeftOperand.IsImplicit))
            {
                var operatorTokenLocation = ((BinaryExpressionSyntax)binaryOperation.Syntax).OperatorToken.GetLocation();
                var diagnostic = Diagnostic.Create(s_rule, operatorTokenLocation);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
