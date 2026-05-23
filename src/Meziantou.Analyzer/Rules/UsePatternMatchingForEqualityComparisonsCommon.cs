using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;
using Meziantou.Analyzer.Internals;

namespace Meziantou.Analyzer.Rules;
internal static class UsePatternMatchingForEqualityComparisonsCommon
{
    public static bool IsNull(IOperation operation)
        => operation.UnwrapConversionOperations() is ILiteralOperation { ConstantValue: { HasValue: true, Value: null } };

    public static bool IsConstantLiteral(IOperation operation)
    {
        if (operation is IConversionOperation { IsImplicit: true, Conversion: { IsImplicit: true, IsNullable: true } } conversionOperation)
        {
            return IsConstantLiteral(conversionOperation.Operand);
        }

        if (operation is ILiteralOperation { ConstantValue.HasValue: true })
            return true;

        if (operation is IFieldReferenceOperation fieldReference && fieldReference.Member.ContainingType.IsEnumeration())
            return true;

        return false;
    }

    public static bool HasImplicitUserDefinedConversion(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversionOperation)
        {
            if (conversionOperation.Conversion.IsUserDefined)
                return true;

            operation = conversionOperation.Operand;
        }

        return false;
    }
}
