﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DontTagInstanceFieldsWithThreadStaticAttributeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.DontTagInstanceFieldsWithThreadStaticAttribute,
        title: "Do not tag instance fields with ThreadStaticAttribute",
        messageFormat: "Do not tag instance fields with ThreadStaticAttribute",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DontTagInstanceFieldsWithThreadStaticAttribute));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(Analyze, SymbolKind.Field);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (field.IsStatic)
            return;

        if (field.HasAttribute(context.Compilation.GetBestTypeByMetadataName("System.ThreadStaticAttribute")))
        {
            context.ReportDiagnostic(s_rule, field);
        }
    }
}
