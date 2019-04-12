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
            messageFormat: "{0}",
            RuleCategories.Usage,
            DiagnosticSeverity.Info,
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

            string reason;
            if (string.Equals(method.Name, nameof(StringBuilder.Append), System.StringComparison.Ordinal))
            {
                if (method.Parameters.Length == 0 || !method.Parameters[0].Type.IsString())
                    return;

                if (!IsOptimizable(context, method.Name, operation.Arguments[0], out reason))
                    return;
            }
            else if (string.Equals(method.Name, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
            {
                if (method.Parameters.Length == 0 || !method.Parameters[0].Type.IsString())
                    return;

                if (!IsOptimizable(context, method.Name, operation.Arguments[0], out reason))
                    return;
            }
            else if (string.Equals(method.Name, nameof(StringBuilder.Insert), System.StringComparison.Ordinal))
            {
                if (method.Parameters.Length == 2 && method.Parameters[1].Type.IsString())
                {
                    if (!IsOptimizable(context, method.Name, operation.Arguments[1], out reason))
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

            context.ReportDiagnostic(s_rule, operation, reason);
        }

        private static bool IsOptimizable(OperationAnalysisContext context, string methodName, IArgumentOperation argument, out string reason)
        {
            reason = default;

            if (argument.ConstantValue.HasValue)
                return false;

            var value = argument.Value;
            if (value is IInterpolatedStringOperation)
            {
                if (TryGetConstStringValue(value, out var constValue))
                {
                    if (constValue.Length == 0)
                    {
                        if (string.Equals(methodName, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
                        {
                            reason = "Remove the useless argument";
                        }
                        else
                        {
                            reason = "Remove this no-op call";
                        }

                        return true;
                    }
                    else if (constValue.Length == 1)
                    {
                        reason = $"Replace {methodName}(string) with {methodName}(char)";
                        return true;
                    }
                    return false;
                }

                reason = $"Replace string interpolation with multiple {methodName} calls";
                return true;
            }
            else if (value.ConstantValue.HasValue && value.ConstantValue.Value is string constValue)
            {
                if (constValue.Length == 0)
                {
                    reason = "Remove this no-op call";
                    return true;
                }
                else if (constValue.Length == 1)
                {
                    if (string.Equals(methodName, nameof(StringBuilder.Append), System.StringComparison.Ordinal))
                    {
                        reason = $"Replace {methodName}(string) with {methodName}(char)";
                        return true;
                    }
                }

                return false;
            }
            else if (value is IBinaryOperation binaryOperation)
            {
                if (binaryOperation.OperatorKind == BinaryOperatorKind.Add && value.Type.IsString())
                {
                    if (IsConstString(binaryOperation.LeftOperand) && IsConstString(binaryOperation.RightOperand))
                        return false;

                    reason = $"Replace the string concatenation by multiple {methodName} calls";
                    return true;
                }
            }
            else if (value is IInvocationOperation invocationOperation)
            {
                var targetMethod = invocationOperation.TargetMethod;
                if (string.Equals(targetMethod.Name, "ToString", System.StringComparison.Ordinal))
                {
                    if (targetMethod.Parameters.Length == 0 && targetMethod.ReturnType.IsString())
                    {
                        if (string.Equals(methodName, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
                        {
                            reason = "Replace with Append().AppendLine()";
                            return true;
                        }
                        else
                        {
                            reason = "Remove the ToString call";
                            return true;
                        }
                    }

                    if (targetMethod.Parameters.Length == 2 &&
                        targetMethod.ReturnType.IsString() &&
                        targetMethod.Parameters[0].Type.IsString() &&
                        targetMethod.Parameters[1].Type.IsEqualsTo(context.Compilation.GetTypeByMetadataName("System.IFormatProvider")))
                    {
                        if (string.Equals(methodName, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
                        {
                            reason = "Use AppendFormat().AppendLine()";
                            return true;
                        }
                        else
                        {
                            reason = "Use AppendFormat";
                            return true;
                        }
                    }
                }
                else if (string.Equals(targetMethod.Name, nameof(string.Substring), System.StringComparison.Ordinal) && targetMethod.ContainingType.IsString())
                {
                    reason = $"Use {methodName}(string, int, int) instead of Substring";
                    return true;
                }
            }

            reason = default;
            return false;
        }

        private static bool IsConstString(IOperation operation)
        {
            return TryGetConstStringValue(operation, out _);
        }

        private static bool TryGetConstStringValue(IOperation operation, out string value)
        {
            var sb = new StringBuilder();
            if (TryGetConstStringValue(operation, sb))
            {
                value = sb.ToString();
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryGetConstStringValue(IOperation operation, StringBuilder sb)
        {
            if (operation == null)
                return false;

            if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is string str)
            {
                sb.Append(str);
                return true;
            }
            else if (operation is IInterpolatedStringOperation interpolationStringOperation)
            {
                foreach (var part in interpolationStringOperation.Parts)
                {
                    if (!TryGetConstStringValue(part, sb))
                        return false;
                }

                return true;
            }
            else if (operation is IInterpolatedStringTextOperation text)
            {
                if (!TryGetConstStringValue(text.Text, sb))
                    return false;

                return true;
            }
            else if (operation is IInterpolatedStringContentOperation interpolated)
            {
                var op = interpolated.Children.SingleOrDefault();
                if (op == null)
                    return false;

                return TryGetConstStringValue(op, sb);
            }

            return false;
        }
    }
}
