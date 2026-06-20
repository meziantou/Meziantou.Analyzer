using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

internal static class UseExclusiveOrOperatorCommon
{
    public static bool TryMatch(IBinaryOperation operation, out IOperation leftOperand, out IOperation rightOperand)
    {
        leftOperand = null!;
        rightOperand = null!;

        if (operation.OperatorKind is not BinaryOperatorKind.ConditionalOr || !operation.Type.IsBoolean())
            return false;

        if (operation.LeftOperand.UnwrapImplicitConversionOperations() is not IBinaryOperation leftOperation)
            return false;

        if (operation.RightOperand.UnwrapImplicitConversionOperations() is not IBinaryOperation rightOperation)
            return false;

        if (!TryGetExclusiveOrBranch(leftOperation, out var leftPositiveOperand, out var leftNegativeOperand))
            return false;

        if (!TryGetExclusiveOrBranch(rightOperation, out var rightPositiveOperand, out var rightNegativeOperand))
            return false;

        var leftPositiveSymbol = GetReferenceSymbol(leftPositiveOperand);
        var leftNegativeSymbol = GetReferenceSymbol(leftNegativeOperand);
        var rightPositiveSymbol = GetReferenceSymbol(rightPositiveOperand);
        var rightNegativeSymbol = GetReferenceSymbol(rightNegativeOperand);
        if (leftPositiveSymbol is null || leftNegativeSymbol is null || rightPositiveSymbol is null || rightNegativeSymbol is null)
            return false;

        if (!SymbolEqualityComparer.Default.Equals(leftPositiveSymbol, rightNegativeSymbol))
            return false;

        if (!SymbolEqualityComparer.Default.Equals(leftNegativeSymbol, rightPositiveSymbol))
            return false;

        leftOperand = leftPositiveOperand;
        rightOperand = leftNegativeOperand;
        return true;
    }

    private static bool TryGetExclusiveOrBranch(IBinaryOperation operation, out IOperation positiveOperand, out IOperation negativeOperand)
    {
        positiveOperand = null!;
        negativeOperand = null!;

        if (operation.OperatorKind is not BinaryOperatorKind.ConditionalAnd || !operation.Type.IsBoolean())
            return false;

        var left = operation.LeftOperand.UnwrapImplicitConversionOperations();
        var right = operation.RightOperand.UnwrapImplicitConversionOperations();
        if (TryGetNegatedOperand(left, out var leftNegatedOperand))
        {
            positiveOperand = right;
            negativeOperand = leftNegatedOperand;
        }
        else if (TryGetNegatedOperand(right, out var rightNegatedOperand))
        {
            positiveOperand = left;
            negativeOperand = rightNegatedOperand;
        }
        else
        {
            return false;
        }

        return IsSimpleBooleanReference(positiveOperand) && IsSimpleBooleanReference(negativeOperand);
    }

    private static bool TryGetNegatedOperand(IOperation operation, out IOperation operand)
    {
        operation = operation.UnwrapImplicitConversionOperations();
        if (operation is IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } unaryOperation)
        {
            operand = unaryOperation.Operand.UnwrapImplicitConversionOperations();
            return true;
        }

        operand = null!;
        return false;
    }

    private static bool IsSimpleBooleanReference(IOperation operation)
    {
        return operation.Type.IsBoolean() && GetReferenceSymbol(operation) is not null;
    }

    private static ISymbol? GetReferenceSymbol(IOperation operation)
    {
        operation = operation.UnwrapImplicitConversionOperations();
        return operation switch
        {
            ILocalReferenceOperation localReference => localReference.Local,
            IParameterReferenceOperation parameterReference => parameterReference.Parameter,
            _ => null,
        };
    }
}
