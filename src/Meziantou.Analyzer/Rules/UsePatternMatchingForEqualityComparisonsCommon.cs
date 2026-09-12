using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

internal static class UsePatternMatchingForEqualityComparisonsCommon
{
    public static bool IsNull(IOperation operation)
        => operation.UnwrapConversions() is ILiteralOperation { ConstantValue: { HasValue: true, Value: null } };

    public static bool IsConstantLiteral(IOperation operation)
    {
        if (operation is IConversionOperation { IsImplicit: true, Conversion: { IsImplicit: true, IsNullable: true } } conversionOperation)
        {
            return IsConstantLiteral(conversionOperation.Operand);
        }

        if (operation is ILiteralOperation { ConstantValue.HasValue: true })
            return true;

        if (operation is IFieldReferenceOperation fieldReference && fieldReference.Member.ContainingType.IsEnum())
            return true;

        return false;
    }

    // The equality operator may implicitly convert the operand (numeric promotion, user-defined conversion, etc.),
    // while the pattern is matched against the type of the operand itself. For instance, 'intValue == 1L' is valid
    // but 'intValue is 1L' is not. The constant pattern is valid only if the constant implicitly converts to the operand type.
    public static bool CanUseConstantPattern(IOperation expressionOperation, IOperation constantOperation)
    {
        if (expressionOperation is not IConversionOperation { IsImplicit: true } conversionOperation)
            return true;

        var semanticModel = expressionOperation.SemanticModel;
        var operandType = conversionOperation.Operand.Type?.GetUnderlyingNullableTypeOrSelf();
        if (semanticModel is null || operandType is null || constantOperation.Syntax is not ExpressionSyntax constantExpression)
            return false;

        // The conversion depends on the value of the constant ('byteValue == 1' is valid, whereas 'byteValue == 300' is not),
        // so it must be classified from the expression instead of the type of the constant
        var conversion = semanticModel.ClassifyConversion(constantExpression, operandType);
        return conversion is { IsImplicit: true, IsUserDefined: false };
    }
}
