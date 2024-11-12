using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventArgsNameShouldEndWithEventArgsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.EventArgsNameShouldEndWithEventArgs,
        title: "Class name should end with 'EventArgs'",
        messageFormat: "Class name should end with 'EventArgs'",
        RuleCategories.Naming,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.EventArgsNameShouldEndWithEventArgs));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.Name is null)
            return;

        if (!symbol.Name.EndsWith("EventArgs", System.StringComparison.Ordinal) && symbol.InheritsFrom(context.Compilation.GetBestTypeByMetadataName("System.EventArgs")))
        {
            context.ReportDiagnostic(Rule, symbol);
        }
    }
}
