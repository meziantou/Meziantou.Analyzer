using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InheritdocShouldNotBeAmbiguousOnTypesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.InheritdocShouldNotBeAmbiguousOnTypes,
        title: "Specify cref for ambiguous inheritdoc on types",
        messageFormat: "Specify 'cref' for '<inheritdoc />' because this type has multiple declared interfaces and no base type",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.InheritdocShouldNotBeAmbiguousOnTypes));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(context =>
        {
            InheritdocOnTypesAnalyzerHelper.Analyze(context, (hasBaseType, interfaceCount) => !hasBaseType && interfaceCount > 1, Rule);
        }, SymbolKind.NamedType);
    }
}
