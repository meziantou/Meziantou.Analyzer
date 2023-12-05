using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseAsyncDelegateForSyncDelegateAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.DoNotUseAsyncDelegateForSyncDelegate,
        title: "Avoid async void method for delegate",
        messageFormat: "Avoid async void method for delegate",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseAsyncDelegateForSyncDelegate));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzerDelegateCreationOperation, OperationKind.DelegateCreation);
    }

    private static void AnalyzerDelegateCreationOperation(OperationAnalysisContext context)
    {
        var operation = (IDelegateCreationOperation)context.Operation;
        if (operation.Type is INamedTypeSymbol { DelegateInvokeMethod: IMethodSymbol delegateInvokeMethod })
        {
            if (!delegateInvokeMethod.ReturnsVoid)
                return;
        }
        else
        {
            // Cannot determine the delegate type
            return;
        }

        if (operation.Target is IAnonymousFunctionOperation { Symbol: { IsAsync: true } })
        {
            context.ReportDiagnostic(s_rule, operation);
        }
    }
}
