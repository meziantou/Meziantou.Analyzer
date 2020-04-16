using System.Collections.Immutable;
using System.Globalization;
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
            title: "Avoid comparison with bool constant",
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
            if (binaryOperation.LeftOperand?.Type is null || binaryOperation.RightOperand?.Type is null)
                return;

            // Operands must be explicit
            if (binaryOperation.LeftOperand.IsImplicit || binaryOperation.RightOperand.IsImplicit)
                return;

            // Neither operand can be dynamic
            if (binaryOperation.LeftOperand.Type.TypeKind == TypeKind.Dynamic || binaryOperation.RightOperand.Type.TypeKind == TypeKind.Dynamic)
                return;

            IOperation nodeToKeep;
            IOperation nodeToRemove;
            if (IsConstantBool(binaryOperation.LeftOperand))
            {
                nodeToKeep = binaryOperation.RightOperand;
                nodeToRemove = binaryOperation.LeftOperand;
            }
            else if (IsConstantBool(binaryOperation.RightOperand))
            {
                nodeToKeep = binaryOperation.LeftOperand;
                nodeToRemove = binaryOperation.RightOperand;
            }
            else
            {
                return;
            }

            // The fixer will need to prefix the remaining operand with '!' if the original comparison is "!= true" or "== false"
            var logicalNotOperatorNeeded = (bool)nodeToRemove.ConstantValue.Value ?
                binaryOperation.OperatorKind == BinaryOperatorKind.NotEquals :
                binaryOperation.OperatorKind == BinaryOperatorKind.Equals;

            var properties = ImmutableDictionary.Create<string, string>()
                .Add("NodeToKeepSpanStart", nodeToKeep.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                .Add("NodeToKeepSpanLength", nodeToKeep.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                .Add("LogicalNotOperatorNeeded", logicalNotOperatorNeeded.ToString());

            var operatorTokenLocation = ((BinaryExpressionSyntax)binaryOperation.Syntax).OperatorToken.GetLocation();
            var diagnostic = Diagnostic.Create(s_rule, operatorTokenLocation, properties);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsConstantBool(IOperation operation)
        {
            return operation.Type.IsBoolean() && operation.ConstantValue.HasValue;
        }
    }
}
