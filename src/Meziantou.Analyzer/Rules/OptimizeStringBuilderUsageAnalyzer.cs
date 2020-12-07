using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class OptimizeStringBuilderUsageAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.OptimizeStringBuilderUsage,
            title: "Optimize StringBuilder usage",
            messageFormat: "{0}",
            RuleCategories.Performance,
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
            if (!method.ContainingType.IsEqualTo(stringBuilderSymbol))
                return;

            if (string.Equals(method.Name, nameof(StringBuilder.Append), System.StringComparison.Ordinal))
            {
                if (method.Parameters.Length == 0 || !method.Parameters[0].Type.IsString())
                    return;

                if (!IsOptimizable(context, operation, method.Name, operation.Arguments[0]))
                    return;
            }
            else if (string.Equals(method.Name, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
            {
                if (method.Parameters.Length == 0 || !method.Parameters[0].Type.IsString())
                    return;

                if (!IsOptimizable(context, operation, method.Name, operation.Arguments[0]))
                    return;
            }
            else if (string.Equals(method.Name, nameof(StringBuilder.Insert), System.StringComparison.Ordinal))
            {
                if (method.Parameters.Length == 2 && method.Parameters[1].Type.IsString())
                {
                    if (!IsOptimizable(context, operation, method.Name, operation.Arguments[1]))
                        return;
                }
            }
        }

        private static ImmutableDictionary<string, string?> CreateProperties(OptimizeStringBuilderUsageData data)
        {
            return ImmutableDictionary.Create<string, string?>().Add("Data", data.ToString());
        }

        private static bool IsOptimizable(OperationAnalysisContext context, IOperation operation, string methodName, IArgumentOperation argument)
        {
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
                            var properties = CreateProperties(OptimizeStringBuilderUsageData.RemoveArgument);
                            context.ReportDiagnostic(s_rule, properties, operation, "Remove the useless argument");
                        }
                        else
                        {
                            var properties = CreateProperties(OptimizeStringBuilderUsageData.RemoveMethod);
                            context.ReportDiagnostic(s_rule, properties, operation, "Remove this no-op call");
                        }

                        return true;
                    }
                    else if (constValue.Length == 1)
                    {
                        var properties = CreateProperties(OptimizeStringBuilderUsageData.ReplaceWithChar)
                            .Add("ConstantValue", constValue);
                        context.ReportDiagnostic(s_rule, properties, argument, $"Replace {methodName}(string) with {methodName}(char)");
                        return true;
                    }
                    return false;
                }
                else
                {
                    if (string.Equals(methodName, nameof(StringBuilder.Insert), System.StringComparison.Ordinal))
                        return false;

                    var properties = CreateProperties(OptimizeStringBuilderUsageData.SplitStringInterpolation);
                    context.ReportDiagnostic(s_rule, properties, operation, $"Replace string interpolation with multiple {methodName} calls");
                    return true;
                }
            }
            else if (TryGetConstStringValue(value, out var constValue))
            {
                if (constValue.Length == 0)
                {
                    if (string.Equals(methodName, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
                    {
                        var properties = CreateProperties(OptimizeStringBuilderUsageData.RemoveArgument);
                        context.ReportDiagnostic(s_rule, properties, operation, "Remove the useless argument");
                    }
                    else
                    {
                        var properties = CreateProperties(OptimizeStringBuilderUsageData.RemoveMethod);
                        context.ReportDiagnostic(s_rule, properties, operation, "Remove this no-op call");
                    }

                    return true;
                }
                else if (constValue.Length == 1)
                {
                    if (string.Equals(methodName, nameof(StringBuilder.Append), System.StringComparison.Ordinal) || string.Equals(methodName, nameof(StringBuilder.Insert), System.StringComparison.Ordinal))
                    {
                        var properties = CreateProperties(OptimizeStringBuilderUsageData.ReplaceWithChar)
                            .Add("ConstantValue", constValue);
                        context.ReportDiagnostic(s_rule, properties, argument, $"Replace {methodName}(string) with {methodName}(char)");
                        return true;
                    }
                }

                return false;
            }
            else if (value is IBinaryOperation binaryOperation)
            {
                if (string.Equals(methodName, nameof(StringBuilder.Append), System.StringComparison.Ordinal) || string.Equals(methodName, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
                {
                    if (binaryOperation.OperatorKind == BinaryOperatorKind.Add && value.Type.IsString())
                    {
                        if (IsConstString(binaryOperation.LeftOperand) && IsConstString(binaryOperation.RightOperand))
                            return false;

                        var properties = CreateProperties(OptimizeStringBuilderUsageData.SplitAddOperator);
                        context.ReportDiagnostic(s_rule, properties, operation, $"Replace the string concatenation by multiple {methodName} calls");
                        return true;
                    }
                }
            }
            else if (value is IInvocationOperation invocationOperation)
            {
                var targetMethod = invocationOperation.TargetMethod;
                if (string.Equals(targetMethod.Name, "ToString", System.StringComparison.Ordinal))
                {
                    if (targetMethod.Parameters.Length == 0 && targetMethod.ReturnType.IsString())
                    {
                        if (invocationOperation.Instance.Type.IsValueType && !IsPrimitive(invocationOperation.Instance.Type))
                            return false;

                        var properties = CreateProperties(OptimizeStringBuilderUsageData.RemoveToString);
                        if (string.Equals(methodName, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
                        {
                            context.ReportDiagnostic(s_rule, properties, operation, "Replace with Append().AppendLine()");
                        }
                        else
                        {
                            context.ReportDiagnostic(s_rule, properties, operation, "Remove the ToString call");
                        }

                        return true;
                    }

                    if (targetMethod.Parameters.Length == 2 &&
                        targetMethod.ReturnType.IsString() &&
                        targetMethod.Parameters[0].Type.IsString() &&
                        targetMethod.Parameters[1].Type.IsEqualTo(context.Compilation.GetTypeByMetadataName("System.IFormatProvider")))
                    {
                        if (string.Equals(methodName, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
                        {
                            var properties = CreateProperties(OptimizeStringBuilderUsageData.ReplaceWithAppendFormat);
                            context.ReportDiagnostic(s_rule, properties, operation, "Use AppendFormat().AppendLine()");
                            return true;
                        }
                        else
                        {
                            var properties = CreateProperties(OptimizeStringBuilderUsageData.ReplaceWithAppendFormat);
                            context.ReportDiagnostic(s_rule, properties, operation, "Use AppendFormat");
                            return true;
                        }
                    }
                }
                else if (string.Equals(targetMethod.Name, nameof(string.Substring), System.StringComparison.Ordinal) && targetMethod.ContainingType.IsString())
                {
                    var properties = CreateProperties(OptimizeStringBuilderUsageData.ReplaceSubstring);
                    context.ReportDiagnostic(s_rule, properties, operation, $"Use {methodName}(string, int, int) instead of Substring");
                    return true;
                }
            }

            return false;
        }

        private static bool IsPrimitive(ITypeSymbol symbol)
        {
            return symbol.SpecialType == SpecialType.System_Boolean
                || symbol.SpecialType == SpecialType.System_Byte
                || symbol.SpecialType == SpecialType.System_Char
                || symbol.SpecialType == SpecialType.System_Decimal
                || symbol.SpecialType == SpecialType.System_Double
                || symbol.SpecialType == SpecialType.System_Int16
                || symbol.SpecialType == SpecialType.System_Int32
                || symbol.SpecialType == SpecialType.System_Int64
                || symbol.SpecialType == SpecialType.System_SByte
                || symbol.SpecialType == SpecialType.System_UInt16
                || symbol.SpecialType == SpecialType.System_UInt32
                || symbol.SpecialType == SpecialType.System_UInt64;
        }

        private static bool IsConstString(IOperation operation)
        {
            return TryGetConstStringValue(operation, out _);
        }

        private static bool TryGetConstStringValue(IOperation operation, [NotNullWhen(true)] out string? value)
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

        internal static string? GetConstStringValue(IOperation operation)
        {
            var sb = new StringBuilder();
            if (TryGetConstStringValue(operation, sb))
                return sb.ToString();

            return null;
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

            if (operation is IInterpolatedStringOperation interpolationStringOperation)
            {
                foreach (var part in interpolationStringOperation.Parts)
                {
                    if (!TryGetConstStringValue(part, sb))
                        return false;
                }

                return true;
            }

            if (operation is IInterpolatedStringTextOperation text)
            {
                if (!TryGetConstStringValue(text.Text, sb))
                    return false;

                return true;
            }

            if (operation is IInterpolatedStringContentOperation interpolated)
            {
                var op = interpolated.Children.SingleOrDefaultIfMultiple();
                if (op == null)
                    return false;

                return TryGetConstStringValue(op, sb);
            }

            if (operation is IMemberReferenceOperation memberReference)
            {
                if (string.Equals(memberReference.Member.Name, nameof(string.Empty), System.StringComparison.Ordinal) && memberReference.Member.ContainingType.IsString())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
