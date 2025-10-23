using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotRemoveOriginalExceptionFromThrowStatementAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotRemoveOriginalExceptionFromThrowStatement,
        title: "Prefer rethrowing an exception implicitly",
        messageFormat: "Prefer rethrowing an exception implicitly",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotRemoveOriginalExceptionFromThrowStatement));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(Analyze, OperationKind.Throw);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        var operation = (IThrowOperation)context.Operation;
        if (operation.Exception is null)
            return;

        if (operation.Exception is not ILocalReferenceOperation localReferenceOperation)
            return;

        var catchOperation = operation.Ancestors().OfType<ICatchClauseOperation>().FirstOrDefault();
        if (catchOperation is null)
            return;

        if (catchOperation.Locals.Contains(localReferenceOperation.Local))
        {
            context.ReportDiagnostic(Rule, operation);
        }
    }
}
