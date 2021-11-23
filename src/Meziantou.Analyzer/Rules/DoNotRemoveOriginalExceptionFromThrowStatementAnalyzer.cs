using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotRemoveOriginalExceptionFromThrowStatementAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.DoNotRemoveOriginalExceptionFromThrowStatement,
        title: "Do not remove original exception",
        messageFormat: "Do not remove original exception",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotRemoveOriginalExceptionFromThrowStatement));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(Analyze, OperationKind.Throw);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        var operation = (IThrowOperation)context.Operation;
        if (operation.Exception == null)
            return;

        if (operation.Exception is not ILocalReferenceOperation localReferenceOperation)
            return;

        var catchOperation = operation.Ancestors().OfType<ICatchClauseOperation>().FirstOrDefault();
        if (catchOperation == null)
            return;

        if (catchOperation.Locals.Contains(localReferenceOperation.Local))
        {
            context.ReportDiagnostic(s_rule, operation);
        }
    }
}
