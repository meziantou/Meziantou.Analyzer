using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AnonymousDelegatesShouldNotBeUsedToUnsubscribeFromEventsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.AnonymousDelegatesShouldNotBeUsedToUnsubscribeFromEvents,
        title: "Anonymous delegates should not be used to unsubscribe from Events",
        messageFormat: "Anonymous delegates should not be used to unsubscribe from Events",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AnonymousDelegatesShouldNotBeUsedToUnsubscribeFromEvents));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeOperation, OperationKind.EventAssignment);
    }

    private static void AnalyzeOperation(OperationAnalysisContext context)
    {
        var operation = (IEventAssignmentOperation)context.Operation;
        if (operation.Adds)
            return;

        var handler = operation.HandlerValue;
        while (handler is IConversionOperation op)
        {
            handler = op.Operand;
        }

        if (handler != null && handler is IDelegateCreationOperation delegateCreation)
        {
            if (delegateCreation.Target is IAnonymousFunctionOperation)
            {
                context.ReportDiagnostic(s_rule, operation);
            }
        }
    }
}
