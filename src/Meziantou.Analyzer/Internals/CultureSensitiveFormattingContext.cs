using System.Linq;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Internals;

internal sealed class CultureSensitiveFormattingContext
{
    public CultureSensitiveFormattingContext(Compilation compilation)
    {
        FormatProviderSymbol = compilation.GetBestTypeByMetadataName("System.IFormatProvider");
        CultureInfoSymbol = compilation.GetBestTypeByMetadataName("System.Globalization.CultureInfo");
        NumberStyleSymbol = compilation.GetBestTypeByMetadataName("System.Globalization.NumberStyles");
        DateTimeStyleSymbol = compilation.GetBestTypeByMetadataName("System.Globalization.DateTimeStyles");
        StringBuilderSymbol = compilation.GetBestTypeByMetadataName("System.Text.StringBuilder");
        StringBuilder_AppendInterpolatedStringHandlerSymbol = compilation.GetBestTypeByMetadataName("System.Text.StringBuilder+AppendInterpolatedStringHandler");
        GuidSymbol = compilation.GetBestTypeByMetadataName("System.Guid");
        EnumSymbol = compilation.GetBestTypeByMetadataName("System.Enum");
        DateTimeOffsetSymbol = compilation.GetBestTypeByMetadataName("System.DateTimeOffset");
        DateOnlySymbol = compilation.GetBestTypeByMetadataName("System.DateOnly");
        TimeOnlySymbol = compilation.GetBestTypeByMetadataName("System.TimeOnly");
        UInt128Symbol = compilation.GetBestTypeByMetadataName("System.UInt128");
        UriSymbol = compilation.GetBestTypeByMetadataName("System.Uri");
        TimeSpanSymbol = compilation.GetBestTypeByMetadataName("System.TimeSpan");
        VersionSymbol = compilation.GetBestTypeByMetadataName("System.Version");
        SystemIFormattableSymbol = compilation.GetBestTypeByMetadataName("System.IFormattable");
        SystemWindowsFontStretchSymbol = compilation.GetBestTypeByMetadataName("System.Windows.FontStretch");
        SystemWindowsMediaBrushSymbol = compilation.GetBestTypeByMetadataName("System.Windows.Media.Brush");
    }

    public INamedTypeSymbol? FormatProviderSymbol { get; }
    public INamedTypeSymbol? CultureInfoSymbol { get; }
    public INamedTypeSymbol? NumberStyleSymbol { get; }
    public INamedTypeSymbol? DateTimeStyleSymbol { get; }
    public INamedTypeSymbol? StringBuilderSymbol { get; }
    public INamedTypeSymbol? StringBuilder_AppendInterpolatedStringHandlerSymbol { get; }
    public INamedTypeSymbol? GuidSymbol { get; }
    public INamedTypeSymbol? EnumSymbol { get; }
    public INamedTypeSymbol? DateTimeOffsetSymbol { get; }
    public INamedTypeSymbol? DateOnlySymbol { get; }
    public INamedTypeSymbol? TimeOnlySymbol { get; }
    public INamedTypeSymbol? UInt128Symbol { get; }
    public INamedTypeSymbol? UriSymbol { get; }
    public INamedTypeSymbol? TimeSpanSymbol { get; }
    public INamedTypeSymbol? VersionSymbol { get; }
    public INamedTypeSymbol? SystemIFormattableSymbol { get; }
    public INamedTypeSymbol? SystemWindowsFontStretchSymbol { get; }
    public INamedTypeSymbol? SystemWindowsMediaBrushSymbol { get; }

    private static bool MustUnwrapNullableOfT(CultureSensitiveOptions options)
    {
        return (options & CultureSensitiveOptions.UnwrapNullableOfT) == CultureSensitiveOptions.UnwrapNullableOfT;
    }

    public bool IsCultureSensitiveOperation(IOperation operation, CultureSensitiveOptions options)
    {
        // Unwrap implicit conversion to Nullable<T>
        if (MustUnwrapNullableOfT(options) && operation is IConversionOperation { Conversion.IsNullable: true, Operand: var conversionOperand })
        {
            operation = conversionOperand;
        }

        if (operation is IInvocationOperation invocation)
        {
            var methodName = invocation.TargetMethod.Name;
            if (methodName is "ToString")
            {
                // Try get the format. Most of ToString have only 1 string parameter to define the format
                IOperation? format = null;
                if (invocation.Arguments.Length > 0)
                {
                    foreach (var arg in invocation.Arguments)
                    {
                        if (arg.Value is { ConstantValue: { HasValue: true, Value: string } })
                        {
                            if (format != null)
                            {
                                format = null;
                                break;
                            }

                            format = arg.Value;
                        }
                    }
                }

                return IsCultureSensitiveType(invocation.TargetMethod.ContainingType, format, instance: invocation.Instance, options);
            }

            if (methodName is "Parse" or "TryParse")
            {
                var type = invocation.TargetMethod.ContainingType;

                // Guid.Parse / Guid.TryParse are culture insensitive
                if (type.IsEqualTo(GuidSymbol))
                    return false;

                // Char.Parse / Char.TryParse are culture insensitive
                if (type.IsChar())
                    return false;

                return IsCultureSensitiveType(type, format: null, instance: null, options);
            }
            else if (methodName is "Append" or "AppendLine" && invocation.TargetMethod.ContainingType.IsEqualTo(StringBuilderSymbol))
            {
                // StringBuilder.AppendLine($"foo{bar}") when bar is a string
                if (invocation.Arguments.Length == 1 && invocation.Arguments[0].Value.Type.IsEqualTo(StringBuilder_AppendInterpolatedStringHandlerSymbol) && !IsCultureSensitiveOperation(invocation.Arguments[0].Value, options))
                    return false;
            }
            else if (methodName is "Format" && invocation.TargetMethod.IsStatic && invocation.TargetMethod.ContainingType.IsString() && invocation.Arguments.Length > 0)
            {
                if (invocation.TargetMethod.Parameters[0].Type.IsEqualTo(FormatProviderSymbol))
                    return false;

                if (invocation.Arguments.Length == 1)
                    return false;

                if (invocation.TargetMethod.Parameters.Length == 2 && invocation.Arguments[1].Parameter?.Type is IArrayTypeSymbol && invocation.Arguments[1].Value is IArrayCreationOperation arrayCreation)
                {
                    var initializer = arrayCreation.Initializer;
                    if (initializer == null)
                        return true;

                    return initializer.ElementValues.Any(arg => IsCultureSensitiveOperation(arg.UnwrapImplicitConversionOperations(), options));
                }
                else
                {
                    return invocation.Arguments.Skip(1).Any(arg => IsCultureSensitiveOperation(arg.Value.UnwrapImplicitConversionOperations(), options));
                }
            }

            if ((options & CultureSensitiveOptions.UseInvocationReturnType) == CultureSensitiveOptions.UseInvocationReturnType)
                return IsCultureSensitiveType(invocation.Type, options);

            return true;
        }

#if CSHARP10_OR_GREATER
        if (operation is IInterpolatedStringHandlerCreationOperation handler)
            return IsCultureSensitiveOperation(handler.Content, options);

        if (operation is IInterpolatedStringAdditionOperation interpolatedStringAddition)
            return IsCultureSensitiveOperation(interpolatedStringAddition.Left, options) || IsCultureSensitiveOperation(interpolatedStringAddition.Right, options);
#endif

        if (operation is IInterpolationOperation content)
            return IsCultureSensitiveType(content.Expression.Type, content.FormatString, content.Expression, options);

        if (operation is IInterpolatedStringTextOperation)
            return false;

#if CSHARP10_OR_GREATER
        if (operation is IInterpolatedStringAppendOperation append)
        {
            if (append.AppendCall is IInvocationOperation appendInvocation)
            {
                if (appendInvocation.Arguments.Length == 1)
                    return IsCultureSensitiveType(appendInvocation.Arguments[0].Value.Type, format: null, instance: null, options);

                if (appendInvocation.Arguments.Length == 2)
                    return IsCultureSensitiveType(appendInvocation.Arguments[0].Value.Type, format: appendInvocation.Arguments[1].Value, instance: null, options);

                // Unknown case
                return true;
            }
            else
            {
                // Unknown case
                return true;
            }
        }
#endif

        if (operation is IInterpolatedStringOperation interpolatedString)
        {
            if (interpolatedString.Parts.Length == 0)
                return false;

            foreach (var part in interpolatedString.Parts)
            {
                if (IsCultureSensitiveOperation(part, options))
                    return true;
            }

            return false;
        }

        if (operation is ILocalReferenceOperation localReference)
            return IsCultureSensitiveType(localReference.Type, options);

        if (operation is IParameterReferenceOperation parameterReference)
            return IsCultureSensitiveType(parameterReference.Type, options);

        if (operation is IMemberReferenceOperation memberReference)
            return IsCultureSensitiveType(memberReference.Type, options);

        if (operation is ILiteralOperation literal)
            return IsCultureSensitiveType(literal.Type, format: null, literal, options);

        if (operation is IConversionOperation conversion)
            return IsCultureSensitiveType(conversion.Type, format: null, instance: operation, options);

        if (operation is IObjectCreationOperation objectCreation)
            return IsCultureSensitiveType(objectCreation.Type, format: null, instance: null, options);

        if (operation is IDefaultValueOperation defaultValue)
            return IsCultureSensitiveType(defaultValue.Type, format: null, instance: null, options);
        
        if (operation is IArrayElementReferenceOperation arrayElementReference)
            return IsCultureSensitiveType(arrayElementReference.Type, format: null, instance: null, options);
        
        if (operation is IBinaryOperation binaryOperation)
            return IsCultureSensitiveType(binaryOperation.Type, format: null, instance: null, options);

        // Unknown operation
        return true;
    }

    private bool IsCultureSensitiveType(ITypeSymbol? typeSymbol, CultureSensitiveOptions options)
    {
        if (typeSymbol == null)
            return true;

        if (MustUnwrapNullableOfT(options))
        {
            typeSymbol = typeSymbol.GetUnderlyingNullableTypeOrSelf();
        }

        if (typeSymbol.IsEnumeration())
            return false;

        if (typeSymbol.SpecialType == SpecialType.System_Boolean)
            return false;

        if (typeSymbol.SpecialType == SpecialType.System_Byte)
            return false;

        if (typeSymbol.SpecialType == SpecialType.System_Char)
            return false;

        if (typeSymbol.SpecialType == SpecialType.System_String)
            return false;

        if (typeSymbol.SpecialType == SpecialType.System_UInt16)
            return false;

        if (typeSymbol.SpecialType == SpecialType.System_UInt32)
            return false;

        if (typeSymbol.SpecialType == SpecialType.System_UInt64)
            return false;

        if (typeSymbol.SpecialType == SpecialType.System_UIntPtr)
            return false;

        if (typeSymbol.IsOrInheritFrom(StringBuilderSymbol))
            return false;

        if (typeSymbol.IsEqualTo(UInt128Symbol))
            return false;

        if (typeSymbol.IsEqualTo(GuidSymbol))
            return false;

        if (typeSymbol.IsEqualTo(VersionSymbol))
            return false;

        if (typeSymbol.IsEqualTo(UriSymbol))
            return false;

        if (typeSymbol.IsEqualTo(SystemWindowsFontStretchSymbol))
            return false;

        if (typeSymbol.IsOrInheritFrom(SystemWindowsMediaBrushSymbol))
            return false;

        return typeSymbol.Implements(SystemIFormattableSymbol);
    }

    private bool IsCultureSensitiveType(ITypeSymbol? symbol, IOperation? format, IOperation? instance, CultureSensitiveOptions options)
    {
        if (!IsCultureSensitiveType(symbol, options))
            return false;

        if (instance != null)
        {
            if (IsConstantPositiveNumber(instance) && format is null or { ConstantValue: { HasValue: true, Value: "" } })
                return false;
        }

        if (symbol.IsDateTime() || symbol.IsEqualToAny(DateTimeOffsetSymbol, DateOnlySymbol, TimeOnlySymbol))
        {
            if (IsInvariantDateTimeFormat(format))
                return false;
        }
        else if (symbol.IsEqualTo(TimeSpanSymbol))
        {
            if (IsInvariantTimeSpanFormat(format))
                return false;
        }

        return true;
    }

    private static bool IsInvariantDateTimeFormat(IOperation? valueOperation)
    {
        return valueOperation is { ConstantValue: { HasValue: true, Value: "o" or "O" or "r" or "R" or "s" or "u" } };
    }

    private static bool IsInvariantTimeSpanFormat(IOperation? valueOperation)
    {
        // note: "c" format is case-sensitive
        return valueOperation == null || valueOperation is { ConstantValue: { HasValue: true, Value: null or "" or "c" or "t" or "T" } };
    }

    // Only negative numbers are culture-sensitive (negative sign)
    // For instance, https://source.dot.net/#System.Private.CoreLib/Int32.cs,8d6f2d8bc0589463
    private static bool IsConstantPositiveNumber(IOperation operation)
    {
        if (operation.Type != null && operation.ConstantValue.HasValue)
        {
            // Only consider types where ToString() is culture-insensitive for positive values
            var constantValue = operation.ConstantValue.Value;
            bool? result = operation.Type.SpecialType switch
            {
                SpecialType.System_Byte => true,
                SpecialType.System_SByte => (sbyte)constantValue! >= 0,
                SpecialType.System_Int16 => (short)constantValue! >= 0,
                SpecialType.System_Int32 => (int)constantValue! >= 0,
                SpecialType.System_Int64 => (long)constantValue! >= 0,
                SpecialType.System_IntPtr when constantValue is int value => value >= 0,
                SpecialType.System_IntPtr when constantValue is long value => value >= 0L,
                SpecialType.System_UInt16 => true,
                SpecialType.System_UInt32 => true,
                SpecialType.System_UInt64 => true,
                SpecialType.System_UIntPtr => true,
                _ => null,
            };
            if (result.HasValue)
                return result.Value;
        }

        if (operation is IMemberReferenceOperation memberReferenceOperation)
        {
            if (memberReferenceOperation.Member.Name == "Count")
                return true;

            if (memberReferenceOperation.Member.Name == "Length")
                return true;

            if (memberReferenceOperation.Member.Name == "LongLength")
                return true;
        }
        else if (operation is IInvocationOperation invocationOperation)
        {
            if (invocationOperation.TargetMethod.Name == "Count")
                return true;

            if (invocationOperation.TargetMethod.Name == "LongCount")
                return true;
        }

        return false;
    }
}
