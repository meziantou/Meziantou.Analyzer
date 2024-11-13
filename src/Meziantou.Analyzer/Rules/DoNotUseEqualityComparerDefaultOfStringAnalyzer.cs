using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseEqualityComparerDefaultOfStringAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseEqualityComparerDefaultOfString,
        title: "Use an explicit StringComparer when possible",
        messageFormat: "Use an overload of '{0}' with a StringComparer",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseEqualityComparerDefaultOfString));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(Analyze, OperationKind.PropertyReference);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        var operation = (IPropertyReferenceOperation)context.Operation;
        if (!string.Equals(operation.Member.Name, nameof(EqualityComparer<string>.Default), StringComparison.Ordinal))
            return;

        var equalityComparerSymbol = context.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.EqualityComparer`1");
        if (equalityComparerSymbol is null)
            return;

        var equalityComparerStringSymbol = equalityComparerSymbol.Construct(context.Compilation.GetSpecialType(SpecialType.System_String));
        if (operation.Member.ContainingType.IsEqualTo(equalityComparerStringSymbol))
        {
            if (operation.IsInNameofOperation())
                return;

            context.ReportDiagnostic(Rule, operation, operation.Member.Name);
        }
    }
}
