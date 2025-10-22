using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStructLayoutAttributeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MissingStructLayoutAttribute,
        title: "Add StructLayoutAttribute",
        messageFormat: "Add StructLayoutAttribute",
        RuleCategories.Performance,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingStructLayoutAttribute));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var attributeType = compilationContext.Compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
            if (attributeType is null)
                return;

            compilationContext.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
        });
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (!symbol.IsValueType || symbol.EnumUnderlyingType is not null) // Only support struct
            return;

        var attributeType = context.Compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
        if (attributeType is null)
            return;

        if (symbol.GetAttributes().Any(attr => attributeType.IsEqualTo(attr.AttributeClass)))
            return;

        var memberCount = 0;
        foreach (var member in symbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.IsConst || member.IsStatic)
                continue;

            if (member.Type.IsReferenceType)
                return; // When a struct contains a reference type field, the layout is automatically changed to Auto

            memberCount++;
        }

        if (memberCount > 1)
        {
            context.ReportDiagnostic(Rule, symbol);
        }
    }
}
