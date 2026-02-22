using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptimizeStringBuilderUsageAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.OptimizeStringBuilderUsage,
        title: "Optimize StringBuilder usage",
        messageFormat: "{0}",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeStringBuilderUsage));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            var analyzerContext = new AnalyzerContext(compilationStartContext.Compilation);
            if (!analyzerContext.IsValid)
                return;

            compilationStartContext.RegisterOperationAction(analyzerContext.Analyze, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext
    {
        private readonly ITypeSymbol? _stringBuilderSymbol;
        private readonly ITypeSymbol? _formatProviderSymbol;
        private readonly HashSet<ISymbol> _appendOverloadTypes = new(SymbolEqualityComparer.Default);
        private readonly bool _hasAppendJoin;

        public AnalyzerContext(Compilation compilation)
        {
            _stringBuilderSymbol = compilation.GetBestTypeByMetadataName("System.Text.StringBuilder");
            if (_stringBuilderSymbol is null)
                return;

            _formatProviderSymbol = compilation.GetBestTypeByMetadataName("System.IFormatProvider");
            _hasAppendJoin = _stringBuilderSymbol.GetMembers("AppendJoin").Length > 0;

            _appendOverloadTypes.AddIfNotNull(_stringBuilderSymbol);
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_Int16));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_Int32));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_Int64));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_UInt16));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_UInt32));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_UInt64));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_Boolean));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_Byte));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_SByte));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_Single));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_Double));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_Decimal));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_String));
            _appendOverloadTypes.AddIfNotNull(compilation.GetSpecialType(SpecialType.System_Char));
            _appendOverloadTypes.AddIfNotNull(compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Char)));
            _appendOverloadTypes.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.ReadOnlySpan`1")?.Construct(compilation.GetSpecialType(SpecialType.System_Char)));
            _appendOverloadTypes.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.ReadOnlyMemory`1")?.Construct(compilation.GetSpecialType(SpecialType.System_Char)));
        }

        public bool IsValid => _stringBuilderSymbol is not null;

        public void Analyze(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.Arguments.Length == 0)
                return;

            var method = operation.TargetMethod;
            if (!method.ContainingType.IsEqualTo(_stringBuilderSymbol))
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

        private bool IsOptimizable(OperationAnalysisContext context, IOperation operation, string methodName, IArgumentOperation argument)
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
                            context.ReportDiagnostic(Rule, properties, operation, "Remove the useless argument");
                        }
                        else
                        {
                            var properties = CreateProperties(OptimizeStringBuilderUsageData.RemoveMethod);
                            context.ReportDiagnostic(Rule, properties, operation, "Remove this no-op call");
                        }

                        return true;
                    }
                    else if (constValue.Length == 1)
                    {
                        var properties = CreateProperties(OptimizeStringBuilderUsageData.ReplaceWithChar)
                            .Add("ConstantValue", constValue);
                        context.ReportDiagnostic(Rule, properties, argument, $"Replace {methodName}(string) with {methodName}(char)");
                        return true;
                    }

                    return false;
                }
                else
                {
                    if (string.Equals(methodName, nameof(StringBuilder.Insert), System.StringComparison.Ordinal))
                        return false;

                    var properties = CreateProperties(OptimizeStringBuilderUsageData.SplitStringInterpolation);
                    context.ReportDiagnostic(Rule, properties, operation, $"Replace string interpolation with multiple {methodName} calls or use an interpolated string");
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
                        context.ReportDiagnostic(Rule, properties, operation, "Remove the useless argument");
                    }
                    else
                    {
                        var properties = CreateProperties(OptimizeStringBuilderUsageData.RemoveMethod);
                        context.ReportDiagnostic(Rule, properties, operation, "Remove this no-op call");
                    }

                    return true;
                }
                else if (constValue.Length == 1)
                {
                    if (string.Equals(methodName, nameof(StringBuilder.Append), System.StringComparison.Ordinal) || string.Equals(methodName, nameof(StringBuilder.Insert), System.StringComparison.Ordinal))
                    {
                        var properties = CreateProperties(OptimizeStringBuilderUsageData.ReplaceWithChar)
                            .Add("ConstantValue", constValue);
                        context.ReportDiagnostic(Rule, properties, argument, $"Replace {methodName}(string) with {methodName}(char)");
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
                        context.ReportDiagnostic(Rule, properties, operation, $"Replace the string concatenation by multiple {methodName} calls");
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
                        if (invocationOperation.Instance?.Type is not null && !_appendOverloadTypes.Contains(invocationOperation.Instance.Type))
                            return false;

                        var properties = CreateProperties(OptimizeStringBuilderUsageData.RemoveToString);
                        if (string.Equals(methodName, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
                        {
                            context.ReportDiagnostic(Rule, properties, operation, "Replace with Append().AppendLine()");
                        }
                        else
                        {
                            context.ReportDiagnostic(Rule, properties, operation, "Remove the ToString call");
                        }

                        return true;
                    }
                    else if (methodName != "Insert"
                        && targetMethod.Parameters.Length == 2
                        && targetMethod.ReturnType.IsString()
                        && targetMethod.Parameters[0].Type.IsString()
                        && targetMethod.Parameters[1].Type.IsEqualTo(_formatProviderSymbol)
                        && invocationOperation.Arguments[0].Value.ConstantValue.HasValue
                        && invocationOperation.Instance is not null)
                    {
                        var properties = CreateProperties(OptimizeStringBuilderUsageData.ReplaceToStringWithAppendFormat);
                        if (string.Equals(methodName, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
                        {
                            context.ReportDiagnostic(Rule, properties, operation, "Replace with AppendFormat().AppendLine()");
                        }
                        else
                        {
                            context.ReportDiagnostic(Rule, properties, operation, "Replace with AppendFormat()");
                        }

                        return true;
                    }
                }
                else if (methodName != "Insert" && string.Equals(targetMethod.Name, nameof(string.Format), System.StringComparison.Ordinal) && targetMethod.ContainingType.IsString() && targetMethod.IsStatic)
                {
                    var properties = CreateProperties(OptimizeStringBuilderUsageData.ReplaceStringFormatWithAppendFormat);
                    if (string.Equals(methodName, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
                    {
                        context.ReportDiagnostic(Rule, properties, operation, "Replace with AppendFormat().AppendLine()");
                    }
                    else
                    {
                        context.ReportDiagnostic(Rule, properties, operation, "Replace with AppendFormat()");
                    }

                    return true;
                }
                else if (methodName != "Insert" && string.Equals(targetMethod.Name, nameof(string.Join), System.StringComparison.Ordinal) && targetMethod.ContainingType.IsString() && targetMethod.IsStatic)
                {
                    // Check if StringBuilder.AppendJoin exists
                    if (_hasAppendJoin)
                    {
                        var properties = CreateProperties(OptimizeStringBuilderUsageData.ReplaceStringJoinWithAppendJoin);
                        if (string.Equals(methodName, nameof(StringBuilder.AppendLine), System.StringComparison.Ordinal))
                        {
                            context.ReportDiagnostic(Rule, properties, operation, "Replace with AppendJoin().AppendLine()");
                        }
                        else
                        {
                            context.ReportDiagnostic(Rule, properties, operation, "Replace with AppendJoin()");
                        }

                        return true;
                    }
                }
                else if (string.Equals(targetMethod.Name, nameof(string.Substring), System.StringComparison.Ordinal) && targetMethod.ContainingType.IsString())
                {
                    var properties = CreateProperties(OptimizeStringBuilderUsageData.ReplaceSubstring);
                    context.ReportDiagnostic(Rule, properties, operation, $"Use {methodName}(string, int, int) or {methodName}(ReadOnlySpan<char>) instead of Substring");
                    return true;
                }
            }

            return false;
        }

        private static bool IsConstString(IOperation operation)
        {
            return TryGetConstStringValue(operation, out _);
        }

        private static bool TryGetConstStringValue(IOperation operation, [NotNullWhen(true)] out string? value)
        {
            var sb = ObjectPool.SharedStringBuilderPool.Get();
            if (OptimizeStringBuilderUsageAnalyzerCommon.TryGetConstStringValue(operation, sb))
            {
                value = sb.ToString();
                ObjectPool.SharedStringBuilderPool.Return(sb);
                return true;
            }

            value = default;
            return false;
        }
    }
}
