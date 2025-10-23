using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotThrowFromFinalizerAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotThrowFromFinalizer,
        title: "Do not throw from a finalizer",
        messageFormat: "Do not throw from a finalizer",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotThrowFromFinalizer));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeFinalizer, SyntaxKind.DestructorDeclaration);
    }

    private static void AnalyzeFinalizer(SyntaxNodeAnalysisContext context)
    {
        var node = (DestructorDeclarationSyntax)context.Node;
        foreach (var throwStatement in node.DescendantNodes().Where(IsThrowStatement))
        {
            context.ReportDiagnostic(Rule, throwStatement);
        }
    }

    private static bool IsThrowStatement(SyntaxNode node) => node.IsKind(SyntaxKind.ThrowStatement);
}
