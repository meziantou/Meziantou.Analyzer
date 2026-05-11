using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DontTagInstanceFieldsWithThreadStaticAttributeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DontTagInstanceFieldsWithThreadStaticAttribute,
        title: "Do not tag instance fields with ThreadStaticAttribute",
        messageFormat: "Do not tag instance fields with ThreadStaticAttribute",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DontTagInstanceFieldsWithThreadStaticAttribute));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var threadStaticAttributeType = compilationContext.Compilation.GetBestTypeByMetadataName("System.ThreadStaticAttribute");
            if (threadStaticAttributeType is null)
                return;

            compilationContext.RegisterSymbolAction(context => Analyze(context, threadStaticAttributeType), SymbolKind.Field);
        });
    }

    private static void Analyze(SymbolAnalysisContext context, ITypeSymbol threadStaticAttributeType)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (field.IsStatic)
            return;

        if (field.HasAttribute(threadStaticAttributeType))
        {
            context.ReportDiagnostic(Rule, field);
        }
    }
}
