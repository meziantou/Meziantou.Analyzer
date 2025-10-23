using System.Collections.Immutable;
using Meziantou.Analyzer.Configurations;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MethodShouldNotBeTooLongAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MethodShouldNotBeTooLong,
        title: "Method is too long",
        messageFormat: "Method is too long ({0})",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodShouldNotBeTooLong));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
                if (node.AccessorList is not null)
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
        if (node is null)
            return;

        var maximumLines = GetMaximumNumberOfLines(context);
        if (maximumLines > 0)
        {
            var location = node.GetLocation();
            var lineSpan = location.GetLineSpan();
            var lines = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line;

            if (GetSkipLocalFunctions(context))
            {
                var localFunctions = node.DescendantNodes(node => !node.IsKind(SyntaxKind.LocalFunctionStatement)).Where(node => node.IsKind(SyntaxKind.LocalFunctionStatement));
                foreach (var localFunction in localFunctions)
                {
                    var firstLine = -1;
                    var lastLine = -1;

                    if (localFunction.HasLeadingTrivia)
                    {
                        firstLine = localFunction.GetLeadingTrivia()[0].GetLocation().GetLineSpan().StartLinePosition.Line;
                    }

                    if (localFunction.HasTrailingTrivia)
                    {
                        lastLine = localFunction.GetTrailingTrivia()[^1].GetLocation().GetLineSpan().EndLinePosition.Line;
                    }

                    if (firstLine < 0 || lastLine < 0)
                    {
                        var functionLocation = localFunction.GetLocation().GetLineSpan();
                        if (firstLine < 0)
                        {
                            firstLine = functionLocation.StartLinePosition.Line;
                        }

                        if (lastLine < 0)
                        {
                            lastLine = functionLocation.EndLinePosition.Line;
                        }
                    }

                    lines -= lastLine - firstLine;
                }
            }

            if (lines > maximumLines)
            {
                context.ReportDiagnostic(Rule, reportNode, $"{lines.ToString(CultureInfo.InvariantCulture)} lines; maximum allowed: {maximumLines.ToString(CultureInfo.InvariantCulture)}");
                return;
            }
        }

        var maximumStatements = GetMaximumNumberOfStatements(context);
        if (maximumStatements > 0)
        {
            var statements = CountStatements(context, node);
            if (statements > maximumStatements)
            {
                context.ReportDiagnostic(Rule, reportNode, $"{statements.ToString(CultureInfo.InvariantCulture)} statements; maximum allowed: {maximumStatements.ToString(CultureInfo.InvariantCulture)}");
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
            if (skipLocalFunctions && node.IsKind(SyntaxKind.LocalFunctionStatement))
                return false;

            return true;
        }

        static bool IsCountableStatement(StatementSyntax statement)
        {
            if (statement is BlockSyntax or LocalFunctionStatementSyntax)
                return false;

            return true;
        }
    }

    private static bool GetSkipLocalFunctions(SyntaxNodeAnalysisContext context)
    {
        var syntaxTree = context.Node?.SyntaxTree;
        if (syntaxTree is not null && context.Options is not null && context.Options.GetConfigurationValue(syntaxTree, $"{Rule.Id}.skip_local_functions", defaultValue: false))
            return true;

        return false;
    }

    private static int GetMaximumNumberOfStatements(SyntaxNodeAnalysisContext context)
    {
        var syntaxTree = context.Node.SyntaxTree;
        return context.Options.GetConfigurationValue(syntaxTree, $"{Rule.Id}.maximum_statements_per_method", defaultValue: 40);
    }

    private static int GetMaximumNumberOfLines(SyntaxNodeAnalysisContext context)
    {
        var syntaxTree = context.Node.SyntaxTree;
        return context.Options.GetConfigurationValue(syntaxTree, $"{Rule.Id}.maximum_lines_per_method", 60);
    }
}
