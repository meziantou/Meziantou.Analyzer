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
            messageFormat: "Method is too long ({0})",
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
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.LocalDeclarationStatement);
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.DestructorDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            switch (context.Node)
            {
                case MethodDeclarationSyntax node:
                    AnalyzeNode(context, node.Body, node.Identifier);
                    AnalyzeNode(context, node.ExpressionBody, node.Identifier);
                    break;

                case LocalFunctionStatementSyntax node:
                    AnalyzeNode(context, node.Body, node.Identifier);
                    AnalyzeNode(context, node.ExpressionBody, node.Identifier);
                    break;

                case PropertyDeclarationSyntax node:
                    if (node.AccessorList != null)
                    {
                        foreach (var accessor in node.AccessorList.Accessors)
                        {
                            AnalyzeNode(context, accessor.Body, accessor.Keyword);
                            AnalyzeNode(context, accessor.ExpressionBody, accessor.Keyword);
                        }
                    }

                    break;

                case ConstructorDeclarationSyntax node:
                    AnalyzeNode(context, node.Body, node.Identifier);
                    AnalyzeNode(context, node.ExpressionBody, node.Identifier);
                    break;

                case DestructorDeclarationSyntax node:
                    AnalyzeNode(context, node.Body, node.Identifier);
                    AnalyzeNode(context, node.ExpressionBody, node.Identifier);
                    break;
            }
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context, SyntaxNode node, SyntaxToken reportNode)
        {
            if (node == null)
                return;

            var maximumLines = GetMaximumNumberOfLines(context);
            if (maximumLines > 0)
            {
                var location = node.GetLocation();
                var lineSpan = location.GetLineSpan();
                var lines = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line;
                if (lines > maximumLines)
                {
                    context.ReportDiagnostic(s_rule, reportNode, $"{lines} lines; maximum allowed: {maximumLines}");
                    return;
                }
            }

            var maximumStatements = GetMaximumNumberOfStatements(context);
            if (maximumStatements > 0)
            {
                var statements = CountStatements(context, node);
                if (statements > maximumStatements)
                {
                    context.ReportDiagnostic(s_rule, reportNode, $"{statements} lines; maximum allowed: {maximumStatements}");
                    return;
                }
            }
        }

        // internal for testing
        internal static int CountStatements(SyntaxNodeAnalysisContext context, SyntaxNode block)
        {
            var skipLocalFunctions = GetSkipLocalFunctions(context);

            return block.DescendantNodesAndSelf(ShouldDescendIntoChildren)
                .OfType<StatementSyntax>()
                .Count(IsCountableStatement);

            bool ShouldDescendIntoChildren(SyntaxNode node)
            {
                if (!skipLocalFunctions && node is LocalFunctionStatementSyntax)
                    return false;

                return true;
            }

            static bool IsCountableStatement(StatementSyntax statement)
            {
                if (statement is BlockSyntax || statement is LocalFunctionStatementSyntax)
                    return false;

                return true;
            }
        }

        private static bool GetSkipLocalFunctions(SyntaxNodeAnalysisContext context)
        {
            if (context.Options != null && context.Options.TryGetConfigurationValue(context.Node.SyntaxTree.FilePath, $"{s_rule.Id}.skipLocalFunctions", out var value) && bool.TryParse(value, out var result))
                return result;

            return false;
        }

        private static int GetMaximumNumberOfStatements(SyntaxNodeAnalysisContext context)
        {
            var file = context.Node.SyntaxTree.FilePath;
            if (context.Options.TryGetConfigurationValue(file, $"{s_rule.Id}.maximumStatementsPerMethod", out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxStatements))
                return maxStatements;

            return 40;
        }

        private static int GetMaximumNumberOfLines(SyntaxNodeAnalysisContext context)
        {
            var file = context.Node.SyntaxTree.FilePath;
            if (context.Options.TryGetConfigurationValue(file, $"{s_rule.Id}.maximumLinesPerMethod", out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxStatements))
                return maxStatements;

            return 40;
        }
    }
}
