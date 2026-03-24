using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UsePartialPropertyInsteadOfPartialMethodForGeneratedRegexAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UsePartialPropertyInsteadOfPartialMethodForGeneratedRegex,
        title: "Use partial property instead of partial method for GeneratedRegex",
        messageFormat: "Use partial property instead of partial method for GeneratedRegex",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UsePartialPropertyInsteadOfPartialMethodForGeneratedRegex));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(ctx =>
        {
            var generatedRegexAttributeSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.GeneratedRegexAttribute");
            if (generatedRegexAttributeSymbol is null)
                return;

            var regexSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.Regex");
            if (regexSymbol is null)
                return;

            ctx.RegisterSymbolAction(symbolCtx => AnalyzeMethod(symbolCtx, generatedRegexAttributeSymbol, regexSymbol), SymbolKind.Method);
        });
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol generatedRegexAttributeSymbol, INamedTypeSymbol regexSymbol)
    {
        var method = (IMethodSymbol)context.Symbol;

        // Check language version from the symbol's actual syntax tree
        var syntaxTree = method.Locations.FirstOrDefault()?.SourceTree;
        if (syntaxTree is null || !syntaxTree.GetCSharpLanguageVersion().IsCSharp13OrAbove())
            return;

        // Must be a partial definition (not the implementation)
        if (!method.IsPartialDefinition)
            return;

        // Must have no parameters
        if (method.Parameters.Length > 0)
            return;

        // Must return Regex
        if (!method.ReturnType.IsEqualTo(regexSymbol))
            return;

        // Must have GeneratedRegex attribute
        if (method.HasAttribute(generatedRegexAttributeSymbol))
        {
            context.ReportDiagnostic(Rule, method);
        }
    }
}
