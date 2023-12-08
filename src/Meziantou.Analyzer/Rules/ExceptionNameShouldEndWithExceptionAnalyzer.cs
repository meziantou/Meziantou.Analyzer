using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExceptionNameShouldEndWithExceptionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ExceptionNameShouldEndWithException,
        title: "Class name should end with 'Exception'",
        messageFormat: "Class name should end with 'Exception'",
        RuleCategories.Naming,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ExceptionNameShouldEndWithException));

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

        if (!symbol.Name.EndsWith("Exception", System.StringComparison.Ordinal) && symbol.InheritsFrom(context.Compilation.GetBestTypeByMetadataName("System.Exception")))
        {
            context.ReportDiagnostic(Rule, symbol);
        }
    }
}
