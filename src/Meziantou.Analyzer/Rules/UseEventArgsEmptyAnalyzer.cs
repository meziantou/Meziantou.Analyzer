﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseEventArgsEmptyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.UseEventArgsEmpty,
        title: "Use EventArgs.Empty",
        messageFormat: "Use EventArgs.Empty instead of new EventArgs()",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseEventArgsEmpty));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(Analyze, OperationKind.ObjectCreation);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        var operation = (IObjectCreationOperation)context.Operation;
        if (operation == null || operation.Constructor == null)
            return;

        if (operation.Arguments.Length > 0)
            return;

        var type = context.Compilation.GetBestTypeByMetadataName("System.EventArgs");
        if (operation.Constructor.ContainingType.IsEqualTo(type))
        {
            context.ReportDiagnostic(s_rule, operation);
        }
    }
}
