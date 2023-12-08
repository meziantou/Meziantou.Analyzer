using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RemoveEmptyStatementAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.RemoveEmptyStatement,
        title: "Remove empty statement",
        messageFormat: "Remove empty statement",
        RuleCategories.Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.RemoveEmptyStatement));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.EmptyStatement);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var node = (EmptyStatementSyntax)context.Node;
        if (node is null)
            return;

        var parent = node.Parent;
        if (parent is null)
            return;

        if (parent.IsKind(SyntaxKind.LabeledStatement))
            return;

        context.ReportDiagnostic(Rule, node);
    }
}
