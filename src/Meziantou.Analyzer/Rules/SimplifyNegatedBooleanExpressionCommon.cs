using Meziantou.Analyzer.Internals;
using Meziantou.Framework.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

internal sealed class SimplifyNegatedBooleanExpressionCommon
{
    private readonly ITypeSymbol? _halfType;

    public SimplifyNegatedBooleanExpressionCommon(Compilation compilation)
    {
        _halfType = compilation.GetBestTypeByMetadataName("System.Half");
    }

    public bool TryMatch(IUnaryOperation operation, out IBinaryOperation binaryOperation)
    {
        binaryOperation = null!;

        if (!IsBuiltInLogicalNot(operation))
            return false;

        if (Unwrap(operation.Operand) is not IBinaryOperation binary)
            return false;

        if (!IsBuiltInConditionalOperator(binary))
            return false;

        if (!TryGetNegationAction(binary.LeftOperand, out var leftAction))
            return false;

        if (!TryGetNegationAction(binary.RightOperand, out var rightAction))
            return false;

        if (leftAction is not NegationAction.RemoveLogicalNot && rightAction is not NegationAction.RemoveLogicalNot)
            return false;

        binaryOperation = binary;
        return true;
    }

    public static BinaryOperatorKind GetOppositeConditionalOperatorKind(BinaryOperatorKind operatorKind)
    {
        return operatorKind switch
        {
            BinaryOperatorKind.ConditionalAnd => BinaryOperatorKind.ConditionalOr,
            BinaryOperatorKind.ConditionalOr => BinaryOperatorKind.ConditionalAnd,
            _ => throw new ArgumentOutOfRangeException(nameof(operatorKind)),
        };
    }

    public bool TryGetNegationAction(IOperation operation, out NegationAction action)
    {
        operation = Unwrap(operation);
        if (!operation.Type.IsBoolean() || operation.Type.TypeKind is TypeKind.Dynamic)
        {
            action = default;
            return false;
        }

        if (operation is IUnaryOperation unaryOperation && IsBuiltInLogicalNot(unaryOperation))
        {
            action = NegationAction.RemoveLogicalNot;
            return true;
        }

        if (operation is IBinaryOperation binaryOperation)
        {
            if (TryGetOppositeComparisonOperatorKind(binaryOperation, out _))
            {
                action = NegationAction.FlipComparison;
                return true;
            }

            if (IsComparisonOperator(binaryOperation))
            {
                action = default;
                return false;
            }
        }

        action = NegationAction.AddLogicalNot;
        return operation.Syntax is not null;
    }

    public bool TryGetOppositeComparisonOperatorKind(IBinaryOperation operation, out BinaryOperatorKind operatorKind)
    {
        operatorKind = operation.OperatorKind switch
        {
            BinaryOperatorKind.Equals => BinaryOperatorKind.NotEquals,
            BinaryOperatorKind.NotEquals => BinaryOperatorKind.Equals,
            BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThanOrEqual,
            BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThan,
            BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThanOrEqual,
            BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThan,
            _ => default,
        };

        if (operatorKind is default(BinaryOperatorKind))
            return false;

        if (operation.OperatorMethod is not null || operation.Type is null || operation.Type.TypeKind is TypeKind.Dynamic)
            return false;

        if (operation.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals)
            return true;

        if (operation.IsLifted)
            return false;

        return !IsFloatingPoint(operation.LeftOperand.Type) && !IsFloatingPoint(operation.RightOperand.Type);
    }

    public static IOperation Unwrap(IOperation operation)
    {
        while (true)
        {
            if (operation is IConversionOperation { IsImplicit: true } conversionOperation)
            {
                operation = conversionOperation.Operand;
                continue;
            }

            if (operation is IParenthesizedOperation parenthesizedOperation)
            {
                operation = parenthesizedOperation.Operand;
                continue;
            }

            return operation;
        }
    }

    private static bool IsBuiltInConditionalOperator(IBinaryOperation operation)
    {
        return operation.OperatorKind is BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr &&
               operation.OperatorMethod is null &&
               operation.Type.IsBoolean();
    }

    private static bool IsComparisonOperator(IBinaryOperation operation)
    {
        return operation.OperatorKind is
            BinaryOperatorKind.Equals or
            BinaryOperatorKind.NotEquals or
            BinaryOperatorKind.LessThan or
            BinaryOperatorKind.LessThanOrEqual or
            BinaryOperatorKind.GreaterThan or
            BinaryOperatorKind.GreaterThanOrEqual;
    }

    private static bool IsBuiltInLogicalNot(IUnaryOperation operation)
    {
        return operation.OperatorKind is UnaryOperatorKind.Not &&
               operation.OperatorMethod is null &&
               operation.Type.IsBoolean();
    }

    private bool IsFloatingPoint(ITypeSymbol? type)
    {
        type = type.GetUnderlyingNullableTypeOrSelf();
        if (type is null)
            return false;

        return type.SpecialType is SpecialType.System_Single or SpecialType.System_Double ||
               type.IsEqualTo(_halfType);
    }

    internal enum NegationAction
    {
        AddLogicalNot,
        RemoveLogicalNot,
        FlipComparison,
    }
}
