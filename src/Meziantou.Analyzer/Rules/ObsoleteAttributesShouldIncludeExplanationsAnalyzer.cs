using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObsoleteAttributesShouldIncludeExplanationsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.ObsoleteAttributesShouldIncludeExplanations,
        title: "Obsolete attributes should include explanations",
        messageFormat: "Obsolete attributes should include explanations",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ObsoleteAttributesShouldIncludeExplanations));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(ctx =>
        {
            var type = ctx.Compilation.GetTypeByMetadataName("System.ObsoleteAttribute");
            if (type == null)
                return;

            ctx.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, type), SymbolKind.Method);
        });
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol obsoleteAttributeTypeSymbol)
    {
        var method = (IMethodSymbol)context.Symbol;
        foreach (var attribute in method.GetAttributes())
        {
            if (!attribute.AttributeClass.IsEqualTo(obsoleteAttributeTypeSymbol))
                continue;

            if (attribute.ConstructorArguments.Length == 0)
            {
                var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
                if (location != null)
                {
                    context.ReportDiagnostic(s_rule, location);
                }
                else
                {
                    context.ReportDiagnostic(s_rule, method);
                }
            }
        }
    }
}
