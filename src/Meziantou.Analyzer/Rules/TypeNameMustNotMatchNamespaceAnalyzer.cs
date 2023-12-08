﻿using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeNameMustNotMatchNamespaceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.TypeNameMustNotMatchNamespace,
        title: "Type name should not match containing namespace",
        messageFormat: "Type name should not match containing namespace",
        RuleCategories.Design,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.TypeNameMustNotMatchNamespace));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;
        if (symbol.ContainingSymbol is INamespaceSymbol ns && string.Equals(ns.Name, symbol.Name, StringComparison.Ordinal))
        {
            context.ReportDiagnostic(Rule, symbol);
        }
    }
}
