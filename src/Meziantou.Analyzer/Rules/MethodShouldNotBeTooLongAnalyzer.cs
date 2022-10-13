using System.Collections.Immutable;
using System.Linq;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MethodShouldNotBeTooLongAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
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
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.LocalFunctionStatement);
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

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context, SyntaxNode? node, SyntaxToken reportNode)
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
                context.ReportDiagnostic(s_rule, reportNode, $"{statements} statements; maximum allowed: {maximumStatements}");
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
        var syntaxTree = context.Node?.SyntaxTree;
        if (syntaxTree != null && context.Options != null && context.Options.GetConfigurationValue(syntaxTree, $"{s_rule.Id}.skip_local_functions", defaultValue: false))
            return true;

        return false;
    }

    private static int GetMaximumNumberOfStatements(SyntaxNodeAnalysisContext context)
    {
        var syntaxTree = context.Node.SyntaxTree;
        return context.Options.GetConfigurationValue(syntaxTree, $"{s_rule.Id}.maximum_statements_per_method", defaultValue: 40);
    }

    private static int GetMaximumNumberOfLines(SyntaxNodeAnalysisContext context)
    {
        var syntaxTree = context.Node.SyntaxTree;
        return context.Options.GetConfigurationValue(syntaxTree, $"{s_rule.Id}.maximum_lines_per_method", 60);
    }
}
