using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis;

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
}
