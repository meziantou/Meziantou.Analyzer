#if CSHARP9_OR_GREATER
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NotPatternShouldBeParenthesizedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.NotPatternShouldBeParenthesized,
        title: "Use parentheses to make not pattern clearer",
        messageFormat: "Use parentheses to make not pattern clearer",
        RuleCategories.Style,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.NotPatternShouldBeParenthesized));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeNotPatternSyntax, SyntaxKind.NotPattern);
    }

    private void AnalyzeNotPatternSyntax(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;
        if (node.Parent is null || node.Parent.IsKind(SyntaxKind.ParenthesizedPattern))
            return;

        if (node.Parent is BinaryPatternSyntax binaryPattern && binaryPattern.OperatorToken.IsKind(SyntaxKind.OrKeyword))
        {
            if (binaryPattern.Left == node)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
            }
        }
    }
}
#endif
