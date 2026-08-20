namespace Meziantou.Analyzer.Internals;

internal sealed class CultureSensitiveFormattingContext(Compilation compilation)
{
    private readonly HashSet<ISymbol> _excludedMethods = CreateExcludedMethods(compilation);
    private readonly HashSet<ISymbol> _cultureInsensitiveMembers = CreateCultureInsensitiveMembers(compilation);

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
    public INamedTypeSymbol? SystemISpanFormattableSymbol { get; } = compilation.GetBestTypeByMetadataName("System.ISpanFormattable");
    public INamedTypeSymbol? SystemWindowsFontStretchSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Windows.FontStretch");
    public INamedTypeSymbol? SystemWindowsMediaBrushSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Windows.Media.Brush");
    public INamedTypeSymbol? NuGetVersioningSemanticVersionSymbol { get; } = compilation.GetBestTypeByMetadataName("NuGet.Versioning.SemanticVersion");
    public INamedTypeSymbol? FormattableStringSymbol { get; } = compilation.GetBestTypeByMetadataName("System.FormattableString");
    public INamedTypeSymbol? InterpolatedStringHandlerAttributeSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute");

    /// <summary>Known .NET interpolated string handlers that format values to strings.</summary>
    private INamedTypeSymbol?[] KnownInterpolatedStringHandlerSymbols { get; } = [
        compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler"),
        compilation.GetBestTypeByMetadataName("System.Text.StringBuilder+AppendInterpolatedStringHandler"),
        compilation.GetBestTypeByMetadataName("System.Diagnostics.Debug+AssertInterpolatedStringHandler"),
        compilation.GetBestTypeByMetadataName("System.Diagnostics.Debug+WriteIfInterpolatedStringHandler"),
        compilation.GetBestTypeByMetadataName("System.MemoryExtensions+TryWriteInterpolatedStringHandler"),
        compilation.GetBestTypeByMetadataName("System.Text.Unicode.Utf8+TryWriteInterpolatedStringHandler"),
    ];

    public bool IsInterpolatedStringHandlerThatFormatsStringValues(ITypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol.IsEqualToAny(KnownInterpolatedStringHandlerSymbols);
    }

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

    private static HashSet<ISymbol> CreateCultureInsensitiveMembers(Compilation compilation)
    {
        var result = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (!AnnotationAttributes.IsCultureInsensitiveAttributeSymbol(attribute.AttributeClass))
                continue;

            if (attribute.ConstructorArguments is not [{ Kind: TypedConstantKind.Primitive, Value: string documentationId }])
                continue;

            foreach (var symbol in DocumentationCommentId.GetSymbolsForDeclarationId(documentationId, compilation))
            {
                result.Add(symbol);
            }
        }

        return result;
    }

    /// <summary>
    /// Indicates whether the value of the operation is annotated with <c>Meziantou.Analyzer.Annotations.CultureInsensitiveAttribute</c>,
    /// which means the value is culture insensitive even when its type is culture sensitive.
    /// </summary>
    private bool IsAnnotatedAsCultureInsensitive(IOperation? operation)
    {
        if (operation is null)
            return false;

        return operation.UnwrapImplicitConversions() switch
        {
            IParameterReferenceOperation parameterReference => IsAnnotatedAsCultureInsensitive(parameterReference.Parameter),
            IMemberReferenceOperation memberReference => IsAnnotatedAsCultureInsensitive(memberReference.Member),
            _ => false,
        };
    }

    /// <summary>
    /// Indicates whether the operation is part of an argument of a parameter annotated with
    /// <c>Meziantou.Analyzer.Annotations.CultureInsensitiveAttribute</c>, which means the value is formatted in a culture-insensitive way.
    /// </summary>
    public bool IsInCultureInsensitiveParameterContext(IOperation operation)
    {
        // Only consider the closest argument, so a value nested in another invocation is not impacted
        for (var current = operation.Parent; current is not null; current = current.Parent)
        {
            if (current is not IArgumentOperation argument)
                continue;

            if (argument.Parameter is not null && IsAnnotatedAsCultureInsensitive(argument.Parameter))
                return true;

            // The compiler generates the calls to AppendFormatted, so continue with the parameter of the annotated method
            if (argument.Parent is IInvocationOperation { Parent: IInterpolatedStringAppendOperation })
                continue;

            return false;
        }

        return false;
    }

    private bool IsAnnotatedAsCultureInsensitive(ISymbol symbol)
    {
        if (_cultureInsensitiveMembers.Count > 0 && (_cultureInsensitiveMembers.Contains(symbol) || _cultureInsensitiveMembers.Contains(symbol.OriginalDefinition)))
            return true;

        if (HasCultureInsensitiveAttribute(symbol.GetAttributes()))
            return true;

        return false;

        static bool HasCultureInsensitiveAttribute(ImmutableArray<AttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                // The constructor with a documentation id only applies to assembly-level attributes
                if (AnnotationAttributes.IsCultureInsensitiveAttributeSymbol(attribute.AttributeClass) && attribute.ConstructorArguments.IsEmpty)
                    return true;
            }

            return false;
        }
    }

    private static bool MustUnwrapNullableOfT(CultureSensitiveOptions options)
    {
        return (options & CultureSensitiveOptions.UnwrapNullableOfT) == CultureSensitiveOptions.UnwrapNullableOfT;
    }

    public static bool IsCultureSensitive(CultureSensitivity cultureSensitivity, CultureSensitiveOptions options)
    {
        if ((cultureSensitivity & CultureSensitivity.CultureSensitive) == CultureSensitivity.CultureSensitive)
            return true;

        if ((options & CultureSensitiveOptions.TreatOpaqueRuntimeTypesAsCultureSensitive) == CultureSensitiveOptions.TreatOpaqueRuntimeTypesAsCultureSensitive &&
            (cultureSensitivity & CultureSensitivity.MaybeCultureSensitiveOpaqueRuntimeType) == CultureSensitivity.MaybeCultureSensitiveOpaqueRuntimeType)
            return true;

        if ((options & CultureSensitiveOptions.TreatUnsealedTypesAsCultureSensitive) == CultureSensitiveOptions.TreatUnsealedTypesAsCultureSensitive &&
            (cultureSensitivity & CultureSensitivity.MaybeCultureSensitiveUnsealedType) == CultureSensitivity.MaybeCultureSensitiveUnsealedType)
            return true;

        return false;
    }

    public static bool IsCultureInsensitive(CultureSensitivity cultureSensitivity, CultureSensitiveOptions options)
    {
        return !IsCultureSensitive(cultureSensitivity, options);
    }

    public CultureSensitivity GetCultureSensitivity(IOperation operation, CultureSensitiveOptions options)
    {
        // Unwrap implicit conversion to Nullable<T>
        if (MustUnwrapNullableOfT(options) && operation is IConversionOperation { Conversion.IsNullable: true, Operand: var conversionOperand })
        {
            operation = conversionOperand;
        }

        if (IsAnnotatedAsCultureInsensitive(operation))
            return CultureSensitivity.CultureInsensitive;

        if (operation is IInvocationOperation invocation)
        {
            if (_excludedMethods.Contains(invocation.TargetMethod))
                return CultureSensitivity.CultureInsensitive;

            if (invocation.HasArgumentOfType(FormatProviderSymbol, inherits: true))
                return CultureSensitivity.CultureInsensitive;

            var methodName = invocation.TargetMethod.Name;
            if (methodName is "ToString")
            {
                // The formatted value is annotated, so the result does not depend on the culture
                if (IsAnnotatedAsCultureInsensitive(invocation.Instance))
                    return CultureSensitivity.CultureInsensitive;

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

                return GetCultureSensitivity(invocation.TargetMethod.ContainingType, format, instance: invocation.Instance, options);
            }

            if (methodName is "Parse" or "TryParse")
            {
                var type = invocation.TargetMethod.ContainingType;

                // Guid.Parse / Guid.TryParse are culture insensitive
                if (type.IsEqualTo(GuidSymbol))
                    return CultureSensitivity.CultureInsensitive;

                // Char.Parse / Char.TryParse are culture insensitive
                if (type.IsChar())
                    return CultureSensitivity.CultureInsensitive;

                return GetCultureSensitivity(type, format: null, instance: null, options);
            }
            else if (methodName is "Append" or "AppendLine" && invocation.TargetMethod.ContainingType.IsEqualTo(StringBuilderSymbol))
            {
                // StringBuilder.AppendLine($"foo{bar}") when bar is a string
                if (invocation.Arguments.Length == 1 && invocation.Arguments[0].Value.Type.IsEqualTo(StringBuilder_AppendInterpolatedStringHandlerSymbol) && GetCultureSensitivity(invocation.Arguments[0].Value, options) == CultureSensitivity.CultureInsensitive)
                    return CultureSensitivity.CultureInsensitive;
            }
            else if (methodName is "AppendFormat" && invocation.TargetMethod.ContainingType.IsEqualTo(StringBuilderSymbol) && invocation.Arguments.Length > 0)
            {
                if (invocation.Arguments.Length == 1)
                    return CultureSensitivity.CultureInsensitive;

                if (invocation.TargetMethod.Parameters.Length == 2 && invocation.Arguments[1].Parameter?.Type is IArrayTypeSymbol && invocation.Arguments[1].Value is IArrayCreationOperation appendFormatArrayCreation)
                {
                    var initializer = appendFormatArrayCreation.Initializer;
                    if (initializer is null)
                        return CultureSensitivity.CultureSensitive;

                    return GetCultureSensitivity(initializer.ElementValues.Select(arg => arg.UnwrapImplicitConversions()), options);
                }
#if ROSLYN_4_14_OR_GREATER
                else if (invocation.TargetMethod.Parameters.Length == 2 && invocation.Arguments[1].Value is ICollectionExpressionOperation appendFormatCollectionExpression)
                {
                    return GetCultureSensitivity(appendFormatCollectionExpression.Elements.Select(arg => arg.UnwrapImplicitConversions()), options);
                }
#endif
                else
                {
                    return GetCultureSensitivity(invocation.Arguments.Skip(1).Select(arg => arg.Value.UnwrapImplicitConversions()), options);
                }
            }
            else if (methodName is "Format" && invocation.TargetMethod.IsStatic && invocation.TargetMethod.ContainingType.IsString() && invocation.Arguments.Length > 0)
            {
                if (invocation.TargetMethod.Parameters[0].Type.IsEqualTo(FormatProviderSymbol))
                    return CultureSensitivity.CultureInsensitive;

                if (invocation.Arguments.Length == 1)
                    return CultureSensitivity.CultureInsensitive;

                if (invocation.TargetMethod.Parameters.Length == 2 && invocation.Arguments[1].Parameter?.Type is IArrayTypeSymbol && invocation.Arguments[1].Value is IArrayCreationOperation arrayCreation)
                {
                    var initializer = arrayCreation.Initializer;
                    if (initializer is null)
                        return CultureSensitivity.CultureSensitive;

                    return GetCultureSensitivity(initializer.ElementValues.Select(arg => arg.UnwrapImplicitConversions()), options);
                }
#if ROSLYN_4_14_OR_GREATER
                else if (invocation.TargetMethod.Parameters.Length == 2 && invocation.Arguments[1].Value is ICollectionExpressionOperation collectionExpression)
                {
                    return GetCultureSensitivity(collectionExpression.Elements.Select(arg => arg.UnwrapImplicitConversions()), options);
                }
#endif
                else
                {
                    return GetCultureSensitivity(invocation.Arguments.Skip(1).Select(arg => arg.Value.UnwrapImplicitConversions()), options);
                }
            }

            // Check interpolated string arguments that are formatted by the invocation.
            var interpolatedStringArgumentCultureSensitivity = GetInterpolatedStringArgumentCultureSensitivity(invocation, options);
            if (interpolatedStringArgumentCultureSensitivity is not null)
                return interpolatedStringArgumentCultureSensitivity.Value;

            if ((options & CultureSensitiveOptions.UseInvocationReturnType) == CultureSensitiveOptions.UseInvocationReturnType)
                return GetCultureSensitivity(invocation.Type, options);

            return CultureSensitivity.CultureSensitive;
        }

        // "value?.ToString()" formats the value when it is not null, so the culture sensitivity is the one of the accessed value
        if (operation is IConditionalAccessOperation conditionalAccess)
            return GetCultureSensitivity(conditionalAccess.WhenNotNull, options);

        if (operation is IInterpolatedStringHandlerCreationOperation handler)
            return GetCultureSensitivity(handler.Content, options);

        if (operation is IInterpolatedStringAdditionOperation interpolatedStringAddition)
            return Combine(GetCultureSensitivity(interpolatedStringAddition.Left, options), GetCultureSensitivity(interpolatedStringAddition.Right, options));

        if (operation is IInterpolationOperation content)
        {
            if (IsAnnotatedAsCultureInsensitive(content.Expression))
                return CultureSensitivity.CultureInsensitive;

            return GetCultureSensitivity(content.Expression.Type, content.FormatString, content.Expression, options);
        }

        if (operation is IInterpolatedStringTextOperation)
            return CultureSensitivity.CultureInsensitive;

        if (operation is IInterpolatedStringAppendOperation append)
        {
            if (append.AppendCall is IInvocationOperation appendInvocation)
            {
                if (appendInvocation.Arguments.Length > 0 && IsAnnotatedAsCultureInsensitive(appendInvocation.Arguments[0].Value))
                    return CultureSensitivity.CultureInsensitive;

                if (appendInvocation.Arguments.Length == 1)
                    return GetCultureSensitivity(appendInvocation.Arguments[0].Value.Type, format: null, instance: null, options);

                if (appendInvocation.Arguments.Length == 2)
                    return GetCultureSensitivity(appendInvocation.Arguments[0].Value.Type, format: appendInvocation.Arguments[1].Value, instance: null, options);

                // Unknown case
                return CultureSensitivity.CultureSensitive;
            }
            else
            {
                // Unknown case
                return CultureSensitivity.CultureSensitive;
            }
        }

        if (operation is IConversionOperation interpolatedConversion && interpolatedConversion.Type.IsEqualTo(FormattableStringSymbol))
            return GetCultureSensitivity(interpolatedConversion.Operand, options);

        if (operation is IInterpolatedStringOperation interpolatedString)
        {
            if (interpolatedString.Parts.Length == 0)
                return CultureSensitivity.CultureInsensitive;

            var cultureSensitivity = CultureSensitivity.CultureInsensitive;
            foreach (var part in interpolatedString.Parts)
            {
                cultureSensitivity = Combine(cultureSensitivity, GetCultureSensitivity(part, options));
            }

            return cultureSensitivity;
        }

        if (operation is ILocalReferenceOperation localReference)
            return GetCultureSensitivity(localReference.Type, options);

        if (operation is IParameterReferenceOperation parameterReference)
            return GetCultureSensitivity(parameterReference.Type, options);

        if (operation is IMemberReferenceOperation memberReference)
            return GetCultureSensitivity(memberReference.Type, options);

        if (operation is ILiteralOperation literal)
            return GetCultureSensitivity(literal.Type, format: null, literal, options);

        if (operation is IConversionOperation conversion)
            return GetCultureSensitivity(conversion.Type, format: null, instance: operation, options);

        if (operation is IObjectCreationOperation objectCreation)
            return GetCultureSensitivity(objectCreation.Type, format: null, instance: null, options);

        if (operation is IDefaultValueOperation defaultValue)
            return GetCultureSensitivity(defaultValue.Type, format: null, instance: null, options);

        if (operation is IArrayElementReferenceOperation arrayElementReference)
            return GetCultureSensitivity(arrayElementReference.Type, format: null, instance: null, options);

        if (operation is IBinaryOperation binaryOperation)
            return GetCultureSensitivity(binaryOperation.Type, format: null, instance: null, options);

        // Unknown operation (conditional expression, coalesce expression, switch expression, await expression, ...).
        // The formatting depends on the type of the value, so use it to determine the culture sensitivity.
        return GetCultureSensitivity(operation.Type, format: null, instance: operation, options);
    }

    public bool IsInInterpolatedStringHandlerContext(IInterpolatedStringOperation operation)
    {
        if (InterpolatedStringHandlerAttributeSymbol is null)
            return false;

        if (IsInterpolatedStringHandlerType(operation.Type))
            return true;

        var current = operation.Parent;
        while (current is not null)
        {
            if (current is IConversionOperation conversionOperation && IsInterpolatedStringHandlerType(conversionOperation.Type))
                return true;

            if (current is IArgumentOperation { Parameter.Type: var parameterType } && IsInterpolatedStringHandlerType(parameterType))
                return true;

            if (current is IVariableInitializerOperation && current.Parent is IVariableDeclaratorOperation { Symbol.Type: var symbolType } && IsInterpolatedStringHandlerType(symbolType))
                return true;

            current = current.Parent;
        }

        return false;
    }

    public bool IsInterpolatedStringHandlerType(ITypeSymbol? typeSymbol)
    {
        return typeSymbol is not null && InterpolatedStringHandlerAttributeSymbol is not null && typeSymbol.HasAttribute(InterpolatedStringHandlerAttributeSymbol);
    }

    public static bool UsesObjectToString(ITypeSymbol? typeSymbol, CancellationToken cancellationToken)
    {
        if (typeSymbol is null)
            return false;

        if (typeSymbol.IsEnum())
            return false;

        if (typeSymbol.IsAnonymousType)
            return false;

        // A non-sealed type may be instantiated by a derived type overriding ToString, unless the whole set of derived types is known (closed hierarchy).
        if (!typeSymbol.IsSealed && !IsClosedHierarchyWithoutToStringOverride(typeSymbol, cancellationToken))
            return false;

        // Look at the whole type hierarchy, as a sealed type may inherit a ToString override from a base class.
        // Stop before System.Object / System.ValueType, whose own ToString() is the "default" implementation this rule warns about.
        for (var current = typeSymbol; current is not null && current.SpecialType is not (SpecialType.System_Object or SpecialType.System_ValueType); current = current.BaseType)
        {
            if (DeclaresToStringOverride(current))
                return false;
        }

        return true;
    }

    private static bool DeclaresToStringOverride(ITypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetMembers(nameof(ToString)).OfType<IMethodSymbol>())
        {
            if (member.Parameters.Length != 0)
                continue;

            if (member.IsOverride)
                return true;
        }

        return false;
    }

    /// <summary>
    /// A <c>closed</c> type cannot be inherited from outside its containing module, so the compiler knows every possible runtime type.
    /// When no type of that hierarchy overrides <c>ToString</c>, the call is guaranteed to use <c>object.ToString</c>, just like for a sealed type.
    /// </summary>
    private static bool IsClosedHierarchyWithoutToStringOverride(ITypeSymbol typeSymbol, CancellationToken cancellationToken)
    {
#if ROSLYN_5_9_OR_GREATER
#pragma warning disable RSEXPERIMENTAL006
        const int MaxClosedHierarchyDepth = 32;

        if (!typeSymbol.IsClosed)
            return false;

        return VisitDerivedTypes(typeSymbol, depth: 0, cancellationToken);

        static bool VisitDerivedTypes(ITypeSymbol typeSymbol, int depth, CancellationToken cancellationToken)
        {
            // Generic closed hierarchies can expand indefinitely, so give up instead of recursing forever
            if (depth > MaxClosedHierarchyDepth)
                return false;

            var derivedTypeInfo = typeSymbol.GetClosedDerivedTypeInfo(cancellationToken);
            if (!derivedTypeInfo.IsComplete)
                return false;

            foreach (var derivedType in derivedTypeInfo.ClosedDerivedTypes)
            {
                if (DeclaresToStringOverride(derivedType))
                    return false;

                if (derivedType.IsSealed)
                    continue;

                // A derived type that is neither sealed nor closed can be inherited by an unknown type overriding ToString
                if (!derivedType.IsClosed)
                    return false;

                if (!VisitDerivedTypes(derivedType, depth + 1, cancellationToken))
                    return false;
            }

            return true;
        }
#pragma warning restore RSEXPERIMENTAL006
#else
        return false;
#endif
    }

    private CultureSensitivity GetCultureSensitivity(ITypeSymbol? typeSymbol, CultureSensitiveOptions options)
    {
        if (typeSymbol is null)
            return CultureSensitivity.MaybeCultureSensitiveOpaqueRuntimeType;

        if (MustUnwrapNullableOfT(options))
        {
            typeSymbol = typeSymbol.GetUnderlyingNullableTypeOrSelf();
        }

        if (typeSymbol is ITypeParameterSymbol typeParameter)
            return GetCultureSensitivity(typeParameter, options);

        if (typeSymbol.IsEnum())
            return CultureSensitivity.CultureInsensitive;

        // The ToString overloads of an enum are declared on System.Enum, so the containing type of an invocation
        // such as 'enumValue.ToString("G")' is System.Enum. They ignore the format provider, so they are culture-insensitive.
        if (typeSymbol.IsEqualTo(EnumSymbol))
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.SpecialType == SpecialType.System_Boolean)
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.SpecialType == SpecialType.System_Byte)
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.SpecialType == SpecialType.System_Char)
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.SpecialType == SpecialType.System_String)
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.SpecialType == SpecialType.System_UInt16)
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.SpecialType == SpecialType.System_UInt32)
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.SpecialType == SpecialType.System_UInt64)
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.SpecialType == SpecialType.System_UIntPtr)
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.IsOrInheritsFrom(StringBuilderSymbol))
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.IsEqualTo(UInt128Symbol))
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.IsEqualTo(GuidSymbol))
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.IsEqualTo(VersionSymbol))
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.IsEqualTo(UriSymbol))
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.IsEqualTo(SystemWindowsFontStretchSymbol))
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.IsOrInheritsFrom(SystemWindowsMediaBrushSymbol))
            return CultureSensitivity.CultureInsensitive;

        if (typeSymbol.IsOrInheritsFrom(NuGetVersioningSemanticVersionSymbol))
            return CultureSensitivity.CultureInsensitive;

        if (!IsCultureSensitiveTypeUsingAttribute(typeSymbol))
            return CultureSensitivity.CultureInsensitive;

#if ROSLYN_5_9_OR_GREATER
        // A union is culture-sensitive when at least one of its case types is culture-sensitive
        if (typeSymbol.IsUnionType() && GetUnionCultureSensitivity(typeSymbol, options, caseType => GetCultureSensitivity(caseType, options)) is { } unionCultureSensitivity)
            return unionCultureSensitivity;
#endif

        if (IsFormattableType(typeSymbol))
            return CultureSensitivity.CultureSensitive;

        if (IsOpaqueRuntimeType(typeSymbol))
            return CultureSensitivity.MaybeCultureSensitiveOpaqueRuntimeType;

        // A sealed type cannot use a more derived runtime type for formatting.
        if (typeSymbol.IsSealed)
            return CultureSensitivity.CultureInsensitive;

        // Formatting may use a runtime type that supports culture-aware formatting.
        return CultureSensitivity.MaybeCultureSensitiveUnsealedType;

        bool IsFormattableType(ITypeSymbol type)
        {
            if (type.IsOrImplements(SystemIFormattableSymbol) || type.IsOrImplements(SystemISpanFormattableSymbol))
                return true;

            // May have ToString(IFormatProvider) even if IFormattable is not implemented directly
            if (HasToStringWithFormatProvider(type))
                return true;

            // For type parameters, also check the constraint types
            if (type is ITypeParameterSymbol typeParameter)
            {
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    if (constraintType.IsOrImplements(SystemIFormattableSymbol) || constraintType.IsOrImplements(SystemISpanFormattableSymbol) || HasToStringWithFormatProvider(constraintType))
                        return true;
                }
            }

            return false;
        }

        bool HasToStringWithFormatProvider(ITypeSymbol type)
            => type.GetAllMembers().OfType<IMethodSymbol>().Any(m => m is { Name: "ToString", IsStatic: false, ReturnType: { SpecialType: SpecialType.System_String }, Parameters: [var param1] } && param1.Type.IsOrInheritsFrom(FormatProviderSymbol) && m.DeclaredAccessibility is Accessibility.Public);
    }

    private CultureSensitivity GetCultureSensitivity(ITypeParameterSymbol typeParameter, CultureSensitiveOptions options)
    {
        if (typeParameter.ConstraintTypes.IsEmpty)
            return CultureSensitivity.MaybeCultureSensitiveOpaqueRuntimeType;

        return GetCultureSensitivity(typeParameter.ConstraintTypes, options);
    }

    private static bool IsOpaqueRuntimeType(ITypeSymbol typeSymbol)
    {
        return typeSymbol.SpecialType == SpecialType.System_Object || typeSymbol.TypeKind == TypeKind.Interface;
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

        if (typeSymbol is ITypeParameterSymbol typeParameter)
        {
            foreach (var constraintType in typeParameter.ConstraintTypes)
            {
                if (!IsCultureSensitiveTypeUsingAttribute(constraintType))
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

        if (typeSymbol is ITypeParameterSymbol typeParameter)
        {
            foreach (var constraintType in typeParameter.ConstraintTypes)
            {
                if (!IsCultureSensitiveTypeUsingAttribute(constraintType, hasFormat, format))
                    return false;
            }
        }

        return true;
    }

    private CultureSensitivity GetCultureSensitivity(ITypeSymbol? symbol, IOperation? format, IOperation? instance, CultureSensitiveOptions options)
    {
        var cultureSensitivity = GetCultureSensitivity(symbol, options);
        if (cultureSensitivity == CultureSensitivity.CultureInsensitive)
            return CultureSensitivity.CultureInsensitive;

        var hasFormatString = format is { ConstantValue.HasValue: true };
        var formatString = format?.ConstantValue.Value as string;

        if (instance is not null)
        {
            if (IsConstantPositiveNumber(instance) && string.IsNullOrEmpty(formatString))
                return CultureSensitivity.CultureInsensitive;
        }

        if (symbol.IsNumberType() && formatString is "B" or ['x', ..] or ['X', ..])
            return CultureSensitivity.CultureInsensitive;

        if (symbol.IsDateTime() || symbol.IsEqualToAny(DateTimeOffsetSymbol, DateOnlySymbol, TimeOnlySymbol))
        {
            if (IsInvariantDateTimeFormat(format))
                return CultureSensitivity.CultureInsensitive;
        }
        else if (symbol.IsEqualTo(TimeSpanSymbol))
        {
            if (IsInvariantTimeSpanFormat(format))
                return CultureSensitivity.CultureInsensitive;
        }

        if (symbol is not null && !IsCultureSensitiveTypeUsingAttribute(symbol, hasFormatString, formatString))
            return CultureSensitivity.CultureInsensitive;

#if ROSLYN_5_9_OR_GREATER
        // The format is applied to the value of the union, so it must be evaluated against the case types
        if (symbol.IsUnionType() && GetUnionCultureSensitivity(symbol, options, caseType => GetCultureSensitivity(caseType, format, instance: null, options)) is { } unionCultureSensitivity)
            return unionCultureSensitivity;
#endif

        return cultureSensitivity;
    }

#if ROSLYN_5_9_OR_GREATER
    /// <summary>Combines the culture sensitivity of all the case types of a union. Returns <see langword="null"/> when the case types cannot be determined.</summary>
    private static CultureSensitivity? GetUnionCultureSensitivity(ITypeSymbol unionType, CultureSensitiveOptions options, Func<ITypeSymbol, CultureSensitivity> getCultureSensitivity)
    {
        var caseTypes = new Stack<ITypeSymbol>(unionType.GetUnionCaseTypes());
        if (caseTypes.Count == 0)
            return null;

        // A case type can be a union, potentially leading to cycles, so the unions are expanded using a set of visited types
        var visitedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default) { unionType };
        var cultureSensitivity = CultureSensitivity.CultureInsensitive;
        while (caseTypes.Count > 0)
        {
            var caseType = caseTypes.Pop();
            if (MustUnwrapNullableOfT(options))
            {
                caseType = caseType.GetUnderlyingNullableTypeOrSelf();
            }

            if (!visitedTypes.Add(caseType))
                continue;

            if (caseType.IsUnionType())
            {
                foreach (var nestedCaseType in caseType.GetUnionCaseTypes())
                {
                    caseTypes.Push(nestedCaseType);
                }

                continue;
            }

            cultureSensitivity = Combine(cultureSensitivity, getCultureSensitivity(caseType));
        }

        return cultureSensitivity;
    }
#endif

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

    private CultureSensitivity GetCultureSensitivity(IEnumerable<IOperation> operations, CultureSensitiveOptions options)
    {
        var cultureSensitivity = CultureSensitivity.CultureInsensitive;
        foreach (var operation in operations)
        {
            cultureSensitivity = Combine(cultureSensitivity, GetCultureSensitivity(operation, options));
        }

        return cultureSensitivity;
    }

    private CultureSensitivity GetCultureSensitivity(IEnumerable<ITypeSymbol> typeSymbols, CultureSensitiveOptions options)
    {
        var cultureSensitivity = CultureSensitivity.CultureInsensitive;
        foreach (var typeSymbol in typeSymbols)
        {
            cultureSensitivity = Combine(cultureSensitivity, GetCultureSensitivity(typeSymbol, options));
        }

        return cultureSensitivity;
    }

    private static CultureSensitivity Combine(CultureSensitivity left, CultureSensitivity right)
    {
        return left | right;
    }

    private CultureSensitivity? GetInterpolatedStringArgumentCultureSensitivity(IInvocationOperation invocation, CultureSensitiveOptions options)
    {
        var hasInterpolatedStringParam = false;
        var cultureSensitivity = CultureSensitivity.CultureInsensitive;

        foreach (var argument in invocation.Arguments)
        {
            var argumentType = argument.Value.Type;
            if (argumentType is null)
                continue;

            if (IsInterpolatedStringType(argumentType))
            {
                hasInterpolatedStringParam = true;
                cultureSensitivity = Combine(cultureSensitivity, GetCultureSensitivity(argument.Value, options));
            }
        }

        return hasInterpolatedStringParam ? cultureSensitivity : null;
    }

    private bool IsInterpolatedStringType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.IsEqualTo(FormattableStringSymbol))
            return true;

        if (InterpolatedStringHandlerAttributeSymbol is not null && typeSymbol.HasAttribute(InterpolatedStringHandlerAttributeSymbol))
            return true;

        return false;
    }
}
