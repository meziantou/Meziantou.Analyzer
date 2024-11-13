using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MarkAttributesWithAttributeUsageAttributeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MarkAttributesWithAttributeUsageAttribute,
        title: "Mark attributes with AttributeUsageAttribute",
        messageFormat: "Mark attributes with AttributeUsageAttribute",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MarkAttributesWithAttributeUsageAttribute));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        var attributeType = context.Compilation.GetBestTypeByMetadataName("System.Attribute");
        var attributeUsageAttributeType = context.Compilation.GetBestTypeByMetadataName("System.AttributeUsageAttribute");
        if (attributeType is null || attributeUsageAttributeType is null)
            return;

        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.IsAbstract)
            return;

        if (!symbol.InheritsFrom(attributeType))
            return;

        if (HasAttributeUsageAttribute(symbol, attributeType, attributeUsageAttributeType))
            return;

        context.ReportDiagnostic(Rule, symbol);
    }

    private static bool HasAttributeUsageAttribute(INamedTypeSymbol? symbol, ITypeSymbol attributeSymbol, ITypeSymbol attributeUsageSymbol)
    {
        while (symbol is not null && !symbol.IsEqualTo(attributeSymbol))
        {
            if (symbol.HasAttribute(attributeUsageSymbol))
                return true;

            symbol = symbol.BaseType;
        }

        return false;
    }
}
