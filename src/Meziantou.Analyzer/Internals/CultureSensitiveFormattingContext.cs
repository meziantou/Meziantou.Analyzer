using System;
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
    public INamedTypeSymbol? TimeSpanSymbol { get; }
    public INamedTypeSymbol? VersionSymbol { get; }
    public INamedTypeSymbol? SystemIFormattableSymbol { get; }
    public INamedTypeSymbol? SystemWindowsFontStretchSymbol { get; }
    public INamedTypeSymbol? SystemWindowsMediaBrushSymbol { get; }

    public bool IsCultureSensitiveOperation(IOperation operation)
    {
        if (operation is IInvocationOperation invocation)
        {
            var methodName = invocation.TargetMethod.Name;
            if (methodName is "ToString")
            {
                // Boolean.ToString(IFormatProvider) should not be used
                if (invocation.TargetMethod.ContainingType.IsBoolean())
                    return false;

                // Char.ToString(IFormatProvider) should not be used
                if (invocation.TargetMethod.ContainingType.IsChar())
                    return false;

                // Guid.ToString(IFormatProvider) should not be used
                if (invocation.TargetMethod.ContainingType.IsEqualTo(GuidSymbol))
                    return false;

                // Enum.ToString(IFormatProvider) should not be used
                if (invocation.TargetMethod.ContainingType.IsEqualTo(EnumSymbol))
                    return false;

                // DateTime.ToString() or DateTimeOffset.ToString() with invariant formats (o, O, r, R, s, u)
                if (invocation.Arguments.Length == 1 && (invocation.TargetMethod.ContainingType.IsDateTime() || invocation.TargetMethod.ContainingType.IsEqualTo(DateTimeOffsetSymbol)))
                {
                    if (IsInvariantDateTimeFormat(invocation.Arguments[0].Value))
                        return false;
                }
            }
            else if (methodName is "Parse" or "TryParse")
            {
                // Guid.Parse / Guid.TryParse are culture insensitive
                if (invocation.TargetMethod.ContainingType.IsEqualTo(GuidSymbol))
                    return false;

                // Char.Parse / Char.TryParse are culture insensitive
                if (invocation.TargetMethod.ContainingType.IsChar())
                    return false;
            }
            else if (methodName is "Append" or "AppendLine" && invocation.TargetMethod.ContainingType.IsEqualTo(StringBuilderSymbol))
            {
                // stringBuilder.AppendLine($"foo{bar}") when bar is a string
                if (invocation.Arguments.Length == 1 && invocation.Arguments[0].Value.Type.IsEqualTo(StringBuilder_AppendInterpolatedStringHandlerSymbol) && !IsCultureSensitiveOperation(invocation.Arguments[0].Value))
                    return false;
            }
        }

#if CSHARP10_OR_GREATER
        if (operation is IInterpolatedStringHandlerCreationOperation handler)
            return IsCultureSensitiveOperation(handler.Content);

        if (operation is IInterpolatedStringAdditionOperation interpolatedStringAddition)
            return IsCultureSensitiveOperation(interpolatedStringAddition.Left) || IsCultureSensitiveOperation(interpolatedStringAddition.Right);
#endif

        if (operation is IInterpolatedStringOperation interpolatedString)
        {
            if (interpolatedString.Parts.Length == 0)
                return false;

            foreach (var part in interpolatedString.Parts)
            {
                if (part is IInterpolatedStringTextOperation)
                    continue;

                if (part is IInterpolationOperation content)
                {
                    if (content.Expression.Type.IsDateTime() || content.Expression.Type.IsEqualTo(DateTimeOffsetSymbol))
                    {
                        if (!IsInvariantDateTimeFormat(content.FormatString))
                            return true;
                    }
                    else if (IsCultureSensitiveType(content.Expression.GetActualType()))
                    {
                        return true;
                    }
                }
#if CSHARP10_OR_GREATER
                else if (part is IInterpolatedStringAppendOperation append)
                {
                    if (append.AppendCall is IInvocationOperation appendInvocation)
                    {
                        if (appendInvocation.Arguments.Length == 1)
                        {
                            if (IsCultureSensitiveType(appendInvocation.Arguments[0].Value.Type))
                                return true;
                        }
                        else if (appendInvocation.Arguments.Length == 2)
                        {
                            var expression = appendInvocation.Arguments[0].Value;
                            if (expression.Type.IsDateTime() || expression.Type.IsEqualTo(DateTimeOffsetSymbol))
                            {
                                if (!IsInvariantDateTimeFormat(appendInvocation.Arguments[1].Value))
                                    return true;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
#endif
                else
                {
                    return true;
                }
            }

            return false;
        }

        if (operation is ILocalReferenceOperation localReference)
            return IsCultureSensitiveType(localReference.Type);

        if (operation is IParameterReferenceOperation parameterReference)
            return IsCultureSensitiveType(parameterReference.Type);

        if (operation is IMemberReferenceOperation memberReference)
            return IsCultureSensitiveType(memberReference.Type);

        if (operation is ILiteralOperation literal)
            return IsCultureSensitiveType(literal.Type);

        // Unknown operation
        return true;
    }

    public bool IsCultureSensitiveType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return true;
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

        if (typeSymbol.IsEqualTo(UInt128Symbol))
            return false;

        if (typeSymbol.IsEqualTo(TimeSpanSymbol))
            return false;

        if (typeSymbol.IsEqualTo(GuidSymbol))
            return false;

        if (typeSymbol.IsOrInheritFrom(VersionSymbol))
            return false;

        if (typeSymbol.IsEqualTo(SystemWindowsFontStretchSymbol))
            return false;

        if (typeSymbol.IsOrInheritFrom(SystemWindowsMediaBrushSymbol))
            return false;

        return typeSymbol.Implements(SystemIFormattableSymbol);
    }

    public bool IsCultureSensitiveType(ITypeSymbol? symbol, IOperation? format, IOperation? instance = null)
    {
        if (!IsCultureSensitiveType(symbol))
            return false;

        if (instance != null)
        {
            if (IsConstantPositiveNumber(instance) && format is null or { ConstantValue: { HasValue: true, Value: "" } })
                return false;
        }

        var isDateTime = symbol.IsDateTime() || symbol.IsEqualToAny(DateTimeOffsetSymbol, DateOnlySymbol, TimeOnlySymbol);
        if (isDateTime)
        {
            if (IsInvariantDateTimeFormat(format))
                return false;
        }

        return true;
    }

    private static bool IsInvariantDateTimeFormat(IOperation? valueOperation)
    {
        return valueOperation is { ConstantValue: { HasValue: true, Value: "o" or "O" or "r" or "R" or "s" or "u" } };
    }

    // Only negative numbers are culture-sensitive (negative sign)
    // For instance, https://source.dot.net/#System.Private.CoreLib/Int32.cs,8d6f2d8bc0589463
    private static bool IsConstantPositiveNumber(IOperation operation)
    {
        if (operation.Type != null && operation.ConstantValue.HasValue)
        {
            var constantValue = operation.ConstantValue.Value;
            bool? result = operation.Type.SpecialType switch
            {
                SpecialType.System_Byte => true,
                SpecialType.System_SByte => (sbyte)constantValue! >= 0,
                SpecialType.System_Int16 => (short)constantValue! >= 0,
                SpecialType.System_Int32 => (int)constantValue! >= 0,
                SpecialType.System_Int64 => (long)constantValue! >= 0,
                SpecialType.System_UInt16 => true,
                SpecialType.System_UInt32 => true,
                SpecialType.System_UInt64 => true,
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
