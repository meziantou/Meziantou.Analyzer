using System;
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlazorPropertyInjectionShouldUseConstructorInjectionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.BlazorPropertyInjectionShouldUseConstructorInjection,
        title: "Use constructor injection instead of [Inject] attribute",
        messageFormat: "Use constructor injection instead of [Inject] attribute",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.BlazorPropertyInjectionShouldUseConstructorInjection));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.IsValid)
            {
                ctx.RegisterSymbolAction(analyzerContext.AnalyzeProperty, SymbolKind.Property);
            }
        });
    }

    private sealed class AnalyzerContext
    {
        private static readonly Version Version9 = new(9, 0, 0, 0);

        public AnalyzerContext(Compilation compilation)
        {
            InjectAttributeSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.InjectAttribute");
            IComponentSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.IComponent");
        }

        public INamedTypeSymbol? InjectAttributeSymbol { get; }
        public INamedTypeSymbol? IComponentSymbol { get; }

        public bool IsValid =>
            InjectAttributeSymbol is not null &&
            IComponentSymbol is not null &&
            InjectAttributeSymbol.ContainingAssembly.Identity.Version >= Version9;

        public void AnalyzeProperty(SymbolAnalysisContext context)
        {
            if (!context.Compilation.GetCSharpLanguageVersion().IsCSharp12OrAbove())
                return;

            var property = (IPropertySymbol)context.Symbol;

            if (!property.HasAttribute(InjectAttributeSymbol, inherits: false))
                return;

            if (!property.ContainingType.IsOrImplements(IComponentSymbol))
                return;

            context.ReportDiagnostic(Rule, property);
        }
    }
}
