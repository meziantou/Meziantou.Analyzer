using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InheritdocShouldHaveSourceOnTypesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.InheritdocShouldHaveSourceOnTypes,
        title: "Do not use inheritdoc on types without inheritance source",
        messageFormat: "Do not use '<inheritdoc />' without 'cref' when this type has no base type and no declared interfaces",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.InheritdocShouldHaveSourceOnTypes));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(context =>
        {
            InheritdocOnTypesAnalyzerHelper.Analyze(context, (hasBaseType, interfaceCount) => !hasBaseType && interfaceCount == 0, Rule);
        }, SymbolKind.NamedType);
    }
}
