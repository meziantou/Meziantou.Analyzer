using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#if ROSLYN_3_8
using System.Linq;
#endif

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReplaceEnumToStringWithNameofAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ReplaceEnumToStringWithNameof,
        title: "Replace constant Enum.ToString with nameof",
        messageFormat: "Replace constant Enum.ToString with nameof",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ReplaceEnumToStringWithNameof));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeInterpolation, OperationKind.Interpolation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var operation = (IInvocationOperation)context.Operation;
        if (operation.TargetMethod.Name != nameof(object.ToString))
            return;

        if (!operation.TargetMethod.ContainingType.IsEqualTo(context.Compilation.GetSpecialType(SpecialType.System_Enum)))
            return;

        if (operation.Instance is not IMemberReferenceOperation expression)
            return;

        if (expression.Member.ContainingType.EnumUnderlyingType is null)
            return;

        if (operation.Arguments.Length > 0)
        {
            var format = operation.Arguments[0].Value;
            if (format is { ConstantValue: { HasValue: true, Value: var formatValue } })
            {
                if (!IsNameFormat(formatValue))
                    return;
            }
            else
            {
                return;
            }
        }

        context.ReportDiagnostic(Rule, operation);
    }

    private static void AnalyzeInterpolation(OperationAnalysisContext context)
    {
        var operation = (IInterpolationOperation)context.Operation;
        if (operation.Expression is not IMemberReferenceOperation expression)
            return;

        if (expression.Member.ContainingType.EnumUnderlyingType is null)
            return;

        if (operation.FormatString is ILiteralOperation { ConstantValue: { HasValue: true, Value: var format } })
        {
            if (!IsNameFormat(format))
                return;
        }

        context.ReportDiagnostic(Rule, operation);
    }


    private static bool IsNameFormat(object? format)
    {
        if (format is null)
            return true;

        if (format is string str && str is "g" or "G" or "f" or "F" or "")
            return true;

        return false;
    }
}
