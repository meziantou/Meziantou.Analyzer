using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class OptimizeStringBuilderUsageAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.OptimizeStringBuilderUsage,
            title: "Optimize StringBuilder usage",
            messageFormat: "Optimize StringBuilder usage",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeStringBuilderUsage));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(Analyze, OperationKind.Invocation);
        }

        private static void Analyze(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.Arguments.Length == 0)
                return;

            var stringBuilderSymbol = context.Compilation.GetTypeByMetadataName("System.Text.StringBuilder");
            if (stringBuilderSymbol == null)
                return;

            var method = operation.TargetMethod;
            if (!method.ContainingType.IsEqualsTo(stringBuilderSymbol))
                return;

            if (string.Equals(method.Name, nameof(StringBuilder.Append), System.StringComparison.Ordinal))
            {
                if (method.Parameters.Length == 0 || !method.Parameters[0].Type.IsString())
                    return;

                if (!IsOptimizable(context, operation.Arguments[0]))
                    return;
            }
            else if (string.Equals(method.Name, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
            {
                if (method.Parameters.Length == 0 || !method.Parameters[0].Type.IsString())
                    return;

                if (!IsOptimizable(context, operation.Arguments[0]))
                    return;
            }
            else if (string.Equals(method.Name, nameof(StringBuilder.Insert), System.StringComparison.Ordinal))
            {
                if (method.Parameters.Length == 2 && method.Parameters[1].Type.IsString())
                {
                    if (!IsOptimizable(context, operation.Arguments[1]))
                        return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation()));
        }

        private static bool IsOptimizable(OperationAnalysisContext context, IArgumentOperation argument)
        {
            if (argument.ConstantValue.HasValue)
                return false;

            // Check for concatenation and FormattableString
            var value = argument.Value;
            if (value is IInterpolatedStringOperation)
            {
                if (IsConstString(value))
                    return false;

                return true;
            }
            else if (value is IBinaryOperation binaryOperation)
            {
                if (value.Type.IsString())
                {
                    if (IsConstString(binaryOperation.LeftOperand) && IsConstString(binaryOperation.RightOperand))
                        return false;

                    return true;
                }
            }
            else if (value is IInvocationOperation invocationOperation)
            {
                var targetMethod = invocationOperation.TargetMethod;
                if (string.Equals(targetMethod.Name, "ToString", System.StringComparison.Ordinal))
                {
                    if (targetMethod.Parameters.Length == 0 && targetMethod.ReturnType.IsString())
                        return true;

                    if (targetMethod.Parameters.Length == 2 &&
                        targetMethod.ReturnType.IsString() &&
                        targetMethod.Parameters[0].Type.IsString() &&
                        targetMethod.Parameters[1].Type.IsEqualsTo(context.Compilation.GetTypeByMetadataName("System.IFormatProvider")))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsConstString(IOperation operation)
        {
            if (operation == null)
                return false;

            if (operation is IInterpolatedStringOperation interpolationStringOperation)
            {
                if (interpolationStringOperation.Parts.All(p => p is IInterpolatedStringTextOperation || IsConstString(p)))
                    return true;

                return false;
            }

            if (operation is IInterpolatedStringContentOperation interpolated)
            {
                var op = interpolated.Children.SingleOrDefault();
                if (op != null)
                {
                    return IsConstString(op);
                }
            }

            return operation.ConstantValue.HasValue && operation.ConstantValue.Value is string;
        }
    }
}
