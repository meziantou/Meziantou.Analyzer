using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidLockingOnPubliclyAccessibleInstanceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.AvoidLockingOnPubliclyAccessibleInstance,
        title: "Avoid locking on publicly accessible instance",
        messageFormat: "Avoid locking on publicly accessible instance",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidLockingOnPubliclyAccessibleInstance));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Lock);
    }

    private static void AnalyzeOperation(OperationAnalysisContext context)
    {
        var operation = (ILockOperation)context.Operation;
        if (operation.LockedValue is ITypeOfOperation)
        {
            context.ReportDiagnostic(Rule, operation.LockedValue);
        }
        else if (operation.LockedValue is IInstanceReferenceOperation)
        {
            if (operation.LockedValue.Type.IsVisibleOutsideOfAssembly())
            {
                context.ReportDiagnostic(Rule, operation.LockedValue);
            }

        }
        else if (operation.LockedValue is IFieldReferenceOperation fieldReferenceOperation && fieldReferenceOperation.Field.IsVisibleOutsideOfAssembly())
        {
            context.ReportDiagnostic(Rule, operation.LockedValue);
        }
        else if (operation.LockedValue is ILocalReferenceOperation localReference && localReference.Local.Type.IsEqualTo(context.Compilation.GetBestTypeByMetadataName("System.Type")))
        {
            context.ReportDiagnostic(Rule, operation.LockedValue);
        }
    }
}
