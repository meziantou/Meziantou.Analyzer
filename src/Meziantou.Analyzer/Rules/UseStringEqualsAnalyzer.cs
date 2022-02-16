using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStringEqualsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.UseStringEquals,
        title: "Use String.Equals instead of equality operator",
        messageFormat: "Use string.Equals instead of {0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringEquals));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.BinaryOperator);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var operation = (IBinaryOperation)context.Operation;
        if (operation.OperatorKind == BinaryOperatorKind.Equals ||
            operation.OperatorKind == BinaryOperatorKind.NotEquals)
        {
            if (operation.LeftOperand.Type.IsString() && operation.RightOperand.Type.IsString())
            {
                if (IsNull(operation.LeftOperand) || IsNull(operation.RightOperand))
                    return;

                if (IsStringEmpty(operation.LeftOperand) || IsStringEmpty(operation.RightOperand))
                    return;

                // EntityFramework Core doesn't support StringComparison and evaluates everything client side...
                // https://github.com/aspnet/EntityFrameworkCore/issues/1222
                if (operation.IsInExpressionContext())
                    return;

                context.ReportDiagnostic(s_rule, operation, $"{operation.OperatorKind} operator");
            }
        }
    }

    private static bool IsNull(IOperation operation)
    {
        return operation.ConstantValue.HasValue && operation.ConstantValue.Value == null;
    }

    private static bool IsStringEmpty(IOperation operation)
    {
        if (operation.ConstantValue.HasValue && (operation.ConstantValue.Value is string str) && string.IsNullOrEmpty(str))
            return true;

        if (operation is IMemberReferenceOperation memberReferenceOperation && memberReferenceOperation.Member.ContainingType.IsString() && memberReferenceOperation.Member.Name == nameof(string.Empty))
            return true;

        return false;
    }
}
