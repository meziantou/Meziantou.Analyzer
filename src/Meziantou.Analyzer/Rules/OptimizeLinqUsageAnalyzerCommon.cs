using Microsoft.CodeAnalysis.CSharp;

namespace Meziantou.Analyzer.Rules;

internal static class OptimizeLinqUsageAnalyzerCommon
{
    internal const string DataKey = "Data";
    internal const string MethodNameKey = "MethodName";
    internal const string ExpectedMethodNameKey = "ExpectedMethodName";
    internal const string FirstOperationStartKey = "FirstOperationStart";
    internal const string FirstOperationLengthKey = "FirstOperationLength";
    internal const string LastOperationStartKey = "LastOperationStart";
    internal const string LastOperationLengthKey = "LastOperationLength";
    internal const string CountOperationStartKey = "CountOperationStart";
    internal const string CountOperationLengthKey = "CountOperationLength";
    internal const string OperandOperationStartKey = "OperandOperationStart";
    internal const string OperandOperationLengthKey = "OperandOperationLength";
    internal const string SkipMinusOneKey = "SkipMinusOne";

    /// <summary>
    /// Indicates if comparing <c>Count()</c> with <paramref name="operand"/> can be replaced by
    /// <c>Take(operand + 1).Count()</c>, which requires <c>operand + 1</c> to be a valid <see cref="int"/>.
    /// </summary>
    /// <param name="operand">The operand compared with the result of <c>Count()</c>.</param>
    /// <param name="takeLocation">The node replaced by the <c>Take</c> invocation, which determines the overflow checking context of <c>operand + 1</c>.</param>
    /// <param name="compilation">The compilation containing <paramref name="takeLocation"/>.</param>
    internal static bool CanUseTakeAndCount(IOperation operand, SyntaxNode takeLocation, Compilation compilation)
    {
        if (operand.ConstantValue.Value is int value)
        {
            // 'int.MaxValue + 1' overflows, and 'Take(int.MaxValue)' would never stop the enumeration earlier anyway
            return value != int.MaxValue;
        }

        // 'operand + 1' throws an OverflowException when the operand is 'int.MaxValue' in a checked context
        return !IsInCheckedContext(takeLocation, compilation);
    }

    private static bool IsInCheckedContext(SyntaxNode node, Compilation compilation)
    {
        foreach (var ancestor in node.Ancestors())
        {
            switch (ancestor.Kind())
            {
                case SyntaxKind.CheckedExpression or SyntaxKind.CheckedStatement:
                    return true;

                case SyntaxKind.UncheckedExpression or SyntaxKind.UncheckedStatement:
                    return false;
            }
        }

        return compilation.Options.CheckOverflow;
    }
}
