using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseEqualityComparerDefaultOfStringAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotUseEqualityComparerDefaultOfString,
            title: "Use StringComparer.Ordinal",
            messageFormat: "Use StringComparer.Ordinal",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseEqualityComparerDefaultOfString));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(Analyze, OperationKind.PropertyReference);
        }

        private static void Analyze(OperationAnalysisContext context)
        {
            var operation = (IPropertyReferenceOperation)context.Operation;
#pragma warning disable MA0024 // Use StringComparer.Ordinal
            if (!string.Equals(operation.Member.Name, nameof(EqualityComparer<string>.Default), StringComparison.Ordinal))
#pragma warning restore MA0024 // Use StringComparer.Ordinal
                return;

            var equalityComparerSymbol = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.EqualityComparer`1");
            if (equalityComparerSymbol == null)
                return;

            var equalityComparerStringSymbol = equalityComparerSymbol.Construct(context.Compilation.GetSpecialType(SpecialType.System_String));
            if (operation.Member.ContainingType.IsEqualsTo(equalityComparerStringSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation()));
            }
        }
    }
}
