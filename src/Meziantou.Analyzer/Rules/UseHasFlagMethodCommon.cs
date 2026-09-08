namespace Meziantou.Analyzer.Rules;

internal static class UseHasFlagMethodCommon
{
    /// <summary>
    /// Determines if both operations reference the same value, so that evaluating them twice is equivalent to evaluating them once.
    /// Only side-effect free references are supported (parameters, locals, fields), so that <c>(value &amp; flag) == flag</c> can safely be replaced by <c>value.HasFlag(flag)</c>.
    /// </summary>
    public static bool AreEquivalentOperands(IOperation? left, IOperation? right)
    {
        if (left is null || right is null)
            return false;

        left = left.UnwrapImplicitConversions();
        right = right.UnwrapImplicitConversions();

        return (left, right) switch
        {
            (IParameterReferenceOperation a, IParameterReferenceOperation b) => a.Parameter.IsEqualTo(b.Parameter),
            (ILocalReferenceOperation a, ILocalReferenceOperation b) => a.Local.IsEqualTo(b.Local),
            (IFieldReferenceOperation a, IFieldReferenceOperation b) => a.Field.IsEqualTo(b.Field) && !a.Field.IsVolatile && (a.Field.IsStatic || AreEquivalentOperands(a.Instance, b.Instance)),
            (IInstanceReferenceOperation a, IInstanceReferenceOperation b) => a.ReferenceKind == b.ReferenceKind && a.Type.IsEqualTo(b.Type),
            _ => false,
        };
    }

    /// <summary>
    /// Determines if replacing the flag check by <c>value.HasFlag(flag)</c> preserves the semantics of the check.
    /// The rewrite evaluates the value operand before the flag operand and drops the duplicated read of the flag,
    /// so it is only valid when evaluating the value operand cannot change the value of the flag.
    /// </summary>
    /// <param name="enumValueOperation">The operand the <c>HasFlag</c> method is called on.</param>
    /// <param name="isConstantFlag">Whether the flag is a compile-time constant, in which case its reads are stable.</param>
    /// <param name="preservesEvaluationOrder">Whether the value operand is already evaluated before every read of the flag.</param>
    public static bool CanRewriteToHasFlag(IOperation enumValueOperation, bool isConstantFlag, bool preservesEvaluationOrder)
    {
        return isConstantFlag || preservesEvaluationOrder || IsSideEffectFree(enumValueOperation);
    }

    /// <summary>
    /// Determines if evaluating the operation cannot have any observable side effect, so that it can be reordered with the read of the flag.
    /// </summary>
    private static bool IsSideEffectFree(IOperation? operation)
    {
        if (operation is null)
            return false;

        if (operation.ConstantValue.HasValue)
            return true;

        return operation switch
        {
            IParameterReferenceOperation or ILocalReferenceOperation or IInstanceReferenceOperation or ILiteralOperation => true,
            IFieldReferenceOperation fieldReference => fieldReference.Field.IsStatic || IsSideEffectFree(fieldReference.Instance),
            IConversionOperation conversion => conversion.OperatorMethod is null && IsSideEffectFree(conversion.Operand),
            IUnaryOperation unary => unary.OperatorMethod is null && IsSideEffectFree(unary.Operand),
            IBinaryOperation binary => binary.OperatorMethod is null && IsSideEffectFree(binary.LeftOperand) && IsSideEffectFree(binary.RightOperand),
            _ => false,
        };
    }
}
