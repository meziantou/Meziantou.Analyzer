using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseEmptyPropertyPatternOnNonNullableValueTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseEmptyPropertyPatternOnNonNullableValueType,
        title: "Do not use empty property patterns with non-nullable value types",
        messageFormat: "This pattern is always '{0}' for non-nullable value types",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseEmptyPropertyPatternOnNonNullableValueType));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            if (context.Compilation.GetCSharpLanguageVersion() < LanguageVersion.CSharp8)
                return;

            context.RegisterOperationAction(AnalyzeIsPattern, OperationKind.IsPattern);
        });
    }

    private static void AnalyzeIsPattern(OperationAnalysisContext context)
    {
        var operation = (IIsPatternOperation)context.Operation;
        if (!TryGetPatternResult(operation.Pattern, out var result))
            return;

        if (!IsNonNullableValueType(operation.Value.GetActualType()))
            return;

        context.ReportDiagnostic(Rule, operation, result ? "true" : "false");
    }

    private static bool TryGetPatternResult(IPatternOperation pattern, out bool result)
    {
        var isNegated = false;
        while (true)
        {
            switch (pattern)
            {
                case INegatedPatternOperation negatedPattern:
                    isNegated = !isNegated;
                    pattern = negatedPattern.Pattern;
                    continue;
                case IRecursivePatternOperation recursivePattern when IsEmptyPropertyPattern(recursivePattern):
                    result = !isNegated;
                    return true;
                default:
                    result = default;
                    return false;
            }
        }
    }

    private static bool IsEmptyPropertyPattern(IRecursivePatternOperation pattern)
    {
        return pattern.Syntax is RecursivePatternSyntax
        {
            Type: null,
            Designation: null,
            PositionalPatternClause: null,
            PropertyPatternClause.Subpatterns.Count: 0,
        };
    }

    private static bool IsNonNullableValueType(ITypeSymbol? type)
    {
        if (type is null)
            return false;

        if (!type.IsValueType)
            return false;

        return type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T;
    }
}
