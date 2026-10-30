namespace Meziantou.Analyzer.Rules;

internal static class FloatingPointToDecimalConversionCommon
{
    /// <summary>
    /// Gets the <c>float</c> or <c>double</c> value that the operation converts to <c>decimal</c>: an explicit conversion,
    /// a <c>decimal</c> constructor, <c>Convert.ToDecimal</c>, or <c>decimal.CreateChecked</c> and its variants.
    /// </summary>
    public static IOperation? GetConvertedFloatingPointValue(IOperation operation) => operation switch
    {
        IConversionOperation conversion when IsDecimal(conversion.Type) && IsFloatingPoint(conversion.Operand.Type) => conversion.Operand,

        IObjectCreationOperation { Constructor: { ContainingType.SpecialType: SpecialType.System_Decimal, Parameters: [var parameter] }, Arguments: [var argument] }
            when IsFloatingPoint(parameter.Type) => argument.Value,

        IInvocationOperation { TargetMethod: { Name: "ToDecimal", ContainingType: { Name: "Convert", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } }, Parameters: [var parameter] }, Arguments: [var argument] }
            when IsFloatingPoint(parameter.Type) => argument.Value,

        IInvocationOperation { TargetMethod: { Name: "CreateChecked" or "CreateSaturating" or "CreateTruncating", ContainingType.SpecialType: SpecialType.System_Decimal, TypeArguments: [var typeArgument] }, Arguments: [var argument] }
            when IsFloatingPoint(typeArgument) => argument.Value,

        _ => null,
    };

    public static bool IsConstant(IOperation value) => value.ConstantValue is { HasValue: true, Value: not null };

    public static bool IsDecimal(ITypeSymbol? type) => GetUnderlyingType(type) is { SpecialType: SpecialType.System_Decimal };

    public static bool IsFloatingPoint(ITypeSymbol? type) => GetUnderlyingType(type) is { SpecialType: SpecialType.System_Double or SpecialType.System_Single };

    private static ITypeSymbol? GetUnderlyingType(ITypeSymbol? type)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [var underlyingType] })
            return underlyingType;

        return type;
    }
}
