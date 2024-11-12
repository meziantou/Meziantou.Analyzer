using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObjectGetTypeOnTypeInstanceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ObjectGetTypeOnTypeInstance,
        title: "GetType() should not be used on System.Type instances",
        messageFormat: "GetType() should not be used on System.Type instances",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ObjectGetTypeOnTypeInstance));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var typeSymbol = context.Compilation.GetBestTypeByMetadataName("System.Type");
            if (typeSymbol is null)
                return;

            context.RegisterOperationAction(context =>
            {
                var operation = (IInvocationOperation)context.Operation;
                if (operation.Instance is not null && operation.TargetMethod.Name == "GetType" && operation.TargetMethod.ContainingType.IsObject())
                {
                    var instanceType = operation.Instance.GetActualType();
                    if (instanceType is null)
                        return;

                    if (instanceType.IsOrInheritFrom(typeSymbol))
                    {
                        context.ReportDiagnostic(Rule, operation);
                    }
                }
            }, OperationKind.Invocation);
        });
    }
}
