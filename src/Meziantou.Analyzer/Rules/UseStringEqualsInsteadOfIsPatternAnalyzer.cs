using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStringEqualsInsteadOfIsPatternAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor RulePattern = new(
        RuleIdentifiers.UseStringEqualsInsteadOfIsPattern,
        title: "Use String.Equals instead of is pattern",
        messageFormat: "Use string.Equals instead of 'is' pattern",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringEqualsInsteadOfIsPattern));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RulePattern);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeIsPattern, OperationKind.IsPattern);
    }

    private static void AnalyzeIsPattern(OperationAnalysisContext context)
    {
        var operation = (IIsPatternOperation)context.Operation;
        AnalyzePattern(context, operation.Pattern);

        static void AnalyzePattern(OperationAnalysisContext context, IPatternOperation pattern)
        {
            if (pattern is IConstantPatternOperation { Value.ConstantValue: { HasValue: true, Value: string { Length: > 0 } } })
            {
                context.ReportDiagnostic(RulePattern, pattern);
            }
            else if (pattern is IRecursivePatternOperation recursivePattern)
            {
                foreach (var p in recursivePattern.PropertySubpatterns)
                {
                    AnalyzePattern(context, p.Pattern);
                }
            }
            else if (pattern is IBinaryPatternOperation binaryPattern)
            {
                AnalyzePattern(context, binaryPattern.LeftPattern);
                AnalyzePattern(context, binaryPattern.RightPattern);
            }
        }
    }
}
