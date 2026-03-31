using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseNullForgivenessAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseNullForgiveness,
        title: "Do not use the null-forgiving operator",
        messageFormat: "Do not use the null-forgiving operator",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseNullForgiveness));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SuppressNullableWarningExpression);
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var node = (PostfixUnaryExpressionSyntax)context.Node;
        if (!node.Operand.IsKind(SyntaxKind.NullLiteralExpression) &&
            !node.Operand.IsKind(SyntaxKind.DefaultLiteralExpression) &&
            !node.Operand.IsKind(SyntaxKind.DefaultExpression))
            return;

        context.ReportDiagnostic(Rule, node);
    }
}
