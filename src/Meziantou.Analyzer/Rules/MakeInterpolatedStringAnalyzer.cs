using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MakeInterpolatedStringAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MakeInterpolatedString,
        title: "Make interpolated string",
        messageFormat: "Make interpolated string",
        RuleCategories.Usage,
        DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MakeInterpolatedString));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeString, SyntaxKind.StringLiteralExpression);
    }

    private void AnalyzeString(SyntaxNodeAnalysisContext context)
    {
        var node = (LiteralExpressionSyntax)context.Node;
        if (IsInterpolatedString(node))
            return;

        if (IsRawString(node))
            return;

        context.ReportDiagnostic(Rule, node);
    }

    private static bool IsRawString(LiteralExpressionSyntax node)
    {
        var token = node.Token.Text;
        return token.Contains("\"\"\"", StringComparison.Ordinal);
    }

    private static bool IsInterpolatedString(LiteralExpressionSyntax node)
    {
        var token = node.Token.Text;
        foreach (var c in token)
        {
            if (c == '"')
                return false;

            if (c == '$')
                return true;
        }

        return false;
    }
}
