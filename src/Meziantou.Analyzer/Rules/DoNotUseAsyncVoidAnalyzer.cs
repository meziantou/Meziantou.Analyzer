using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseAsyncVoidAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseAsyncVoid,
        title: "Do not use async void methods",
        messageFormat: "Do not use async void methods",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseAsyncVoid));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        context.RegisterOperationAction(AnalyzeLocalFunction, OperationKind.LocalFunction);
    }

    private void AnalyzeLocalFunction(OperationAnalysisContext context)
    {
        var operation = (ILocalFunctionOperation)context.Operation;
        if (operation.Symbol is { ReturnsVoid: true, IsAsync: true })
        {
            context.ReportDiagnostic(Rule, operation);
        }
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = (IMethodSymbol)context.Symbol;
        if (symbol is { ReturnsVoid: true, IsAsync: true })
        {
            context.ReportDiagnostic(Rule, symbol);
        }
    }
}