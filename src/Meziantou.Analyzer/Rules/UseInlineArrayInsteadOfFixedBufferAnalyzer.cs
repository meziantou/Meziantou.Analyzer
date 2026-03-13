using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseInlineArrayInsteadOfFixedBufferAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseInlineArrayInsteadOfFixedBuffer,
        title: "Use InlineArray instead of fixed-size buffers",
        messageFormat: "Use InlineArray instead of fixed-size buffer '{0}'",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseInlineArrayInsteadOfFixedBuffer));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            if (!context.Compilation.GetCSharpLanguageVersion().IsCSharp12OrAbove())
                return;

            var inlineArrayAttributeSymbol = context.Compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.InlineArrayAttribute");
            if (inlineArrayAttributeSymbol is null)
                return;

            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        });
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (!field.IsFixedSizeBuffer || field.IsImplicitlyDeclared)
            return;

        context.ReportDiagnostic(Rule, field, field.Name);
    }
}
