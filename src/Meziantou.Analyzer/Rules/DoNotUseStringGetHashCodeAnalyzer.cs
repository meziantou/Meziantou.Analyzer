using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseStringGetHashCodeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.DoNotUseStringGetHashCode,
        title: "Use StringComparer.GetHashCode instead of string.GetHashCode",
        messageFormat: "Use an explicit StringComparer to compute hash codes",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseStringGetHashCode));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(context =>
        {
            var stringComparisonSymbol = context.Compilation.GetBestTypeByMetadataName("System.StringComparison");
            if (stringComparisonSymbol == null)
                return;

            context.RegisterOperationAction(context => AnalyzeInvocation(context, stringComparisonSymbol), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol stringComparisonSymbol)
    {
        var operation = (IInvocationOperation)context.Operation;
        if (operation.TargetMethod.Name is "GetHashCode" && operation.TargetMethod.ContainingType.IsString())
        {
            if (operation.HasArgumentOfType(stringComparisonSymbol))
                return;

            context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name);
        }
    }
}
