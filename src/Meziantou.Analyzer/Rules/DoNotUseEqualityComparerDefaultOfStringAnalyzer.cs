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

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var equalityComparerSymbol = compilationContext.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.EqualityComparer`1");
            if (equalityComparerSymbol is null)
                return;

            var equalityComparerStringSymbol = equalityComparerSymbol.Construct(compilationContext.Compilation.GetSpecialType(SpecialType.System_String));
            compilationContext.RegisterOperationAction(context => Analyze(context, equalityComparerStringSymbol), OperationKind.PropertyReference);
        });
    }

    private static void Analyze(OperationAnalysisContext context, ITypeSymbol equalityComparerStringSymbol)
    {
        var operation = (IPropertyReferenceOperation)context.Operation;
        if (!string.Equals(operation.Member.Name, nameof(EqualityComparer<>.Default), StringComparison.Ordinal))
            return;

        if (operation.Member.ContainingType.IsEqualTo(equalityComparerStringSymbol))
        {
            if (operation.IsInNameofOperation())
                return;

            context.ReportDiagnostic(Rule, operation, operation.Member.Name);
        }
    }
}
