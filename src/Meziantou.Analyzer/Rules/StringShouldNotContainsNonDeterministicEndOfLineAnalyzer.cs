using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class StringShouldNotContainsNonDeterministicEndOfLineAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.StringShouldNotContainsNonDeterministicEndOfLine,
            title: "String contains an implicit end of line character",
            messageFormat: "String contains an implicit end of line character",
            RuleCategories.Usage,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.StringShouldNotContainsNonDeterministicEndOfLine));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeStringLiteralExpression, SyntaxKind.StringLiteralExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInterpolatedVerbatimStringStartToken, SyntaxKind.InterpolatedStringExpression);
        }

        private void AnalyzeInterpolatedVerbatimStringStartToken(SyntaxNodeAnalysisContext context)
        {
            var node = (InterpolatedStringExpressionSyntax)context.Node;
            foreach (var item in node.Contents)
            {
                if (item is InterpolatedStringTextSyntax text)
                {
                    var position = text.GetLocation().GetLineSpan();
                    if (position.StartLinePosition.Line != position.EndLinePosition.Line)
                    {
                        context.ReportDiagnostic(s_rule, node);
                        return;
                    }
                }
            }
        }

        private void AnalyzeStringLiteralExpression(SyntaxNodeAnalysisContext context)
        {
            var node = (LiteralExpressionSyntax)context.Node;
            var position = node.GetLocation().GetLineSpan();
            if (position.StartLinePosition.Line != position.EndLinePosition.Line)
            {
                context.ReportDiagnostic(s_rule, node);
            }
        }
    }
}
