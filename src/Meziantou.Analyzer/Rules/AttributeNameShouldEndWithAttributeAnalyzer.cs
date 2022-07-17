using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AttributeNameShouldEndWithAttributeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.AttributeNameShouldEndWithAttribute,
        title: "Class name should end with 'Attribute'",
        messageFormat: "Class name should end with 'Attribute'",
        RuleCategories.Naming,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AttributeNameShouldEndWithAttribute));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.Name == null)
            return;

        if (!symbol.Name.EndsWith("Attribute", System.StringComparison.Ordinal) && symbol.InheritsFrom(context.Compilation.GetBestTypeByMetadataName("System.Attribute")))
        {
            context.ReportDiagnostic(s_rule, symbol);
        }
    }
}
