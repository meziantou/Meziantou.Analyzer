using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis.CSharp;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodShouldNotBeTooLongAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.MethodShouldNotBeTooLong,
            title: "Method is too long",
            messageFormat: "Method is too long ({0} statements; maximum: {1})",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodShouldNotBeTooLong));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var maximumStatements = GetMaximumNumberOfStatements(context);
            if (maximumStatements <= 0)
                return;

            var node = (MethodDeclarationSyntax)context.Node;
            if (node.Body != null)
            {
                var statements = CountStatements(node.Body);
                if (statements > maximumStatements)
                {
                    context.ReportDiagnostic(s_rule, node.Identifier, statements.ToString(CultureInfo.InvariantCulture), maximumStatements.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        // internal for testing
        internal static int CountStatements(BlockSyntax block)
        {
#if DEBUG
            var statements = block.DescendantNodes().OfType<StatementSyntax>().ToList();
#endif
            return block.DescendantNodes()
                .OfType<StatementSyntax>()
                .Where(statement => !(statement is BlockSyntax))
                .Count();
        }

        private static int GetMaximumNumberOfStatements(SyntaxNodeAnalysisContext context)
        {
            var file = context.Node.SyntaxTree.FilePath;
            if (context.Options.TryGetConfigurationValue(file, "meziantou.maximumStatementsPerMethod", out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxStatements))
                return maxStatements;

            return -1;
        }
    }
}
