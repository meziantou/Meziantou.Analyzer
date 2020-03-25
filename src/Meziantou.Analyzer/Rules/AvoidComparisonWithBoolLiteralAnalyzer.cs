using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

        private static readonly ImmutableArray<SyntaxKind> s_comparisonExpressionKinds =
            ImmutableArray.Create(SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeComparisonWithBool, s_comparisonExpressionKinds);
        }

        private static void AnalyzeComparisonWithBool(SyntaxNodeAnalysisContext context)
        {
            var binaryExpressionSyntax = (BinaryExpressionSyntax)context.Node;

            if (binaryExpressionSyntax.Left is null || binaryExpressionSyntax.Right is null)
                return;

            if (binaryExpressionSyntax.Left.IsKind(SyntaxKind.TrueLiteralExpression) ||
                binaryExpressionSyntax.Left.IsKind(SyntaxKind.FalseLiteralExpression) ||
                binaryExpressionSyntax.Right.IsKind(SyntaxKind.TrueLiteralExpression) ||
                binaryExpressionSyntax.Right.IsKind(SyntaxKind.FalseLiteralExpression))
            {
                context.ReportDiagnostic(s_rule, binaryExpressionSyntax.OperatorToken);
            }
        }
    }
}
