using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotThrowExceptionFromFinallyBlockAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotThrowExceptionFromFinallyBlock,
            title: "Do not throw an exception from a finally block",
            messageFormat: "Do not throw an exception from a finally block",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotThrowExceptionFromFinallyBlock));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeFinallyClause, SyntaxKind.FinallyClause);
        }

        private static void AnalyzeFinallyClause(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is FinallyClauseSyntax finallyClause))
                return;

            var finallyBlock = finallyClause.Block;
            if (finallyBlock is null)
                return;

            var result = context.SemanticModel.AnalyzeControlFlow(finallyBlock);
            if (!result.Succeeded)
                return;

            if (!result.EndPointIsReachable)
            {
                var throwStatement = finallyBlock.Statements.FirstOrDefault(s => s.GetType() == typeof(ThrowStatementSyntax));
                if (throwStatement is null)
                    return;

                context.ReportDiagnostic(s_rule, throwStatement);
            }
        }
    }
}
