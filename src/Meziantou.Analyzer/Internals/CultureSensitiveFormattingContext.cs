using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Internals;

internal sealed class CultureSensitiveFormattingContext(Compilation compilation)
{
    private readonly HashSet<ISymbol> _excludedMethods = CreateExcludedMethods(compilation);

    public INamedTypeSymbol? FormatProviderSymbol { get; } = compilation.GetBestTypeByMetadataName("System.IFormatProvider");
    public INamedTypeSymbol? CultureInfoSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Globalization.CultureInfo");
    public INamedTypeSymbol? NumberStyleSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Globalization.NumberStyles");
    public INamedTypeSymbol? DateTimeStyleSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Globalization.DateTimeStyles");
    public INamedTypeSymbol? StringBuilderSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Text.StringBuilder");
    public INamedTypeSymbol? StringBuilder_AppendInterpolatedStringHandlerSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Text.StringBuilder+AppendInterpolatedStringHandler");
    public INamedTypeSymbol? GuidSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Guid");
    public INamedTypeSymbol? EnumSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Enum");
    public INamedTypeSymbol? DateTimeOffsetSymbol { get; } = compilation.GetBestTypeByMetadataName("System.DateTimeOffset");
    public INamedTypeSymbol? DateOnlySymbol { get; } = compilation.GetBestTypeByMetadataName("System.DateOnly");
    public INamedTypeSymbol? TimeOnlySymbol { get; } = compilation.GetBestTypeByMetadataName("System.TimeOnly");
    public INamedTypeSymbol? UInt128Symbol { get; } = compilation.GetBestTypeByMetadataName("System.UInt128");
    public INamedTypeSymbol? UriSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Uri");
    public INamedTypeSymbol? TimeSpanSymbol { get; } = compilation.GetBestTypeByMetadataName("System.TimeSpan");
    public INamedTypeSymbol? VersionSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Version");
    public INamedTypeSymbol? SystemIFormattableSymbol { get; } = compilation.GetBestTypeByMetadataName("System.IFormattable");
    public INamedTypeSymbol? SystemWindowsFontStretchSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Windows.FontStretch");
    public INamedTypeSymbol? SystemWindowsMediaBrushSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Windows.Media.Brush");
    public INamedTypeSymbol? NuGetVersioningSemanticVersionSymbol { get; } = compilation.GetBestTypeByMetadataName("NuGet.Versioning.SemanticVersion");

    private static HashSet<ISymbol> CreateExcludedMethods(Compilation compilation)
    {
        var result = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        AddDocumentationId(result, compilation, "M:System.Convert.ToChar(System.String)");
        AddDocumentationId(result, compilation, "M:System.Convert.ToChar(System.Object)");
        AddDocumentationId(result, compilation, "M:System.Convert.ToBoolean(System.String)");
        AddDocumentationId(result, compilation, "M:System.Convert.ToBoolean(System.Object)");
        return result;

        static void AddDocumentationId(HashSet<ISymbol> result, Compilation compilation, string id)
        {
            foreach (var item in DocumentationCommentId.GetSymbolsForDeclarationId(id, compilation))
            {
                result.Add(item);
            }
        }
    }

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
            if (_excludedMethods.Contains(invocation.TargetMethod))
                return false;

            if (invocation.HasArgumentOfType(FormatProviderSymbol, inherits: true))
                return false;

            var methodName = invocation.TargetMethod.Name;
            if (methodName is "ToString")
            {
                // Try get the format. Most of ToString have only 1 string parameter to define the format
                IOperation? format = null;
                if (invocation.Arguments.Length > 0)
                {
                    foreach (var arg in invocation.Arguments)
                    {
                        if (arg.Value is { ConstantValue: { HasValue: true, Value: string } } or IConversionOperation { Type.SpecialType: SpecialType.System_String, ConstantValue: { HasValue: true, Value: null } })
                        {
                            if (format is not null)
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
                    if (initializer is null)
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
        if (typeSymbol is null)
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

        if (typeSymbol.IsOrInheritFrom(NuGetVersioningSemanticVersionSymbol))
            return false;

        if (!IsFormattableType(typeSymbol))
            return false;

        if (!IsCultureSensitiveTypeUsingAttribute(typeSymbol))
            return false;

        return true;

        bool IsFormattableType(ITypeSymbol type)
        {
            if (type.Implements(SystemIFormattableSymbol))
                return true;

            // May have ToString(IFormatProvider) even if IFormattable is not implemented directly
            if (type.GetAllMembers().OfType<IMethodSymbol>().Any(m => m is { Name: "ToString", IsStatic: false, ReturnType: { SpecialType: SpecialType.System_String }, Parameters: [var param1] } && param1.Type.IsOrInheritFrom(FormatProviderSymbol) && m.DeclaredAccessibility is Accessibility.Public))
                return true;

            return false;
        }
    }

    private bool IsCultureSensitiveTypeUsingAttribute(ITypeSymbol typeSymbol)
    {
        var attributes = typeSymbol.GetAttributes();
        foreach (var attr in attributes)
        {
            if (!AnnotationAttributes.IsCultureSensitiveAttributeSymbol(attr.AttributeClass))
                continue;

            if (attr.ConstructorArguments.IsEmpty)
                return false; // no format is set, so the type is culture insensitive
        }

        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (!AnnotationAttributes.IsCultureSensitiveAttributeSymbol(attribute.AttributeClass))
                continue;

            if (attribute.ConstructorArguments.IsEmpty)
                continue;

            if (attribute.ConstructorArguments[0].Value is INamedTypeSymbol attributeType && attributeType.IsEqualTo(typeSymbol))
            {
                if (attribute.ConstructorArguments.Length == 1)
                    return false;
            }
        }

        return true;
    }

    private bool IsCultureSensitiveTypeUsingAttribute(ITypeSymbol typeSymbol, bool hasFormat, string? format)
    {
        var attributes = typeSymbol.GetAttributes();
        foreach (var attr in attributes)
        {
            if (!AnnotationAttributes.IsCultureSensitiveAttributeSymbol(attr.AttributeClass))
                continue;

            if (attr.ConstructorArguments.IsEmpty)
                return false; // no format is set, so the type is culture insensitive

            var attrValue = attr.ConstructorArguments[0].Value;
            if (!hasFormat)
            {
                if (attrValue is bool isDefaultFormatCultureInsensitive && isDefaultFormatCultureInsensitive)
                    return false;

                continue;
            }

            var attrFormat = attrValue as string;
            if (attrFormat == format)
                return false; // no format is set, so the type is culture insensitive
        }

        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (!AnnotationAttributes.IsCultureSensitiveAttributeSymbol(attribute.AttributeClass))
                continue;

            if (attribute.ConstructorArguments.IsEmpty)
                continue;

            if (attribute.ConstructorArguments[0].Value is INamedTypeSymbol attributeType && attributeType.IsEqualTo(typeSymbol))
            {
                if (attribute.ConstructorArguments.Length == 1)
                    return false;

                var attrValue = attribute.ConstructorArguments[1].Value;
                if (!hasFormat)
                {
                    if (attrValue is bool isDefaultFormatCultureInsensitive && isDefaultFormatCultureInsensitive)
                        return false;

                    continue;
                }

                var attrFormat = attrValue as string;
                if (attrFormat == format)
                    return false; // no format is set, so the type is culture insensitive
            }
        }

        return true;
    }

    private bool IsCultureSensitiveType(ITypeSymbol? symbol, IOperation? format, IOperation? instance, CultureSensitiveOptions options)
    {
        if (!IsCultureSensitiveType(symbol, options))
            return false;

        var hasFormatString = format is { ConstantValue.HasValue: true };
        var formatString = format?.ConstantValue.Value as string;

        if (instance is not null)
        {
            if (IsConstantPositiveNumber(instance) && string.IsNullOrEmpty(formatString))
                return false;
        }

        if (symbol.IsNumberType() && formatString is "B" or ['x', ..] or ['X', ..])
            return false;

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

        if (symbol is not null && !IsCultureSensitiveTypeUsingAttribute(symbol, hasFormatString, formatString))
            return false;

        return true;
    }

    private static bool IsInvariantDateTimeFormat(IOperation? valueOperation)
    {
        return valueOperation is { ConstantValue: { HasValue: true, Value: "o" or "O" or "r" or "R" or "s" or "u" } };
    }

    private static bool IsInvariantTimeSpanFormat(IOperation? valueOperation)
    {
        // note: "c" format is case-sensitive
        return valueOperation is null or { ConstantValue: { HasValue: true, Value: null or "" or "c" or "t" or "T" } };
    }

    // Only negative numbers are culture-sensitive (negative sign)
    // For instance, https://source.dot.net/#System.Private.CoreLib/Int32.cs,8d6f2d8bc0589463
    private static bool IsConstantPositiveNumber(IOperation operation)
    {
        if (operation.Type is not null && operation.ConstantValue.HasValue)
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
