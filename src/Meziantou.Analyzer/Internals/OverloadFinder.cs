namespace Meziantou.Analyzer.Internals;

internal sealed class OverloadFinder(Compilation compilation)
{
    /// <summary>
    /// The name of the diagnostic property containing the namespace the code fix must import to call the overload, when the
    /// overload is an extension method declared in a namespace that is not imported. See <see cref="GetNamespaceToImport"/>.
    /// </summary>
    public const string NamespaceToImportPropertyName = "NamespaceToImport";

    private readonly ITypeSymbol? _obsoleteSymbol = compilation.GetBestTypeByMetadataName("System.ObsoleteAttribute");
    private readonly ITypeSymbol? _experimentalSymbol = compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.ExperimentalAttribute");
    private readonly INamedTypeSymbol? _ienumerableOfTSymbol = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
    private readonly INamedTypeSymbol? _halfSymbol = compilation.GetBestTypeByMetadataName("System.Half");
    private readonly Lazy<Dictionary<string, List<IMethodSymbol>>> _extensionMethodsByName = new(() => CreateExtensionMethodsByName(compilation));

    private static ReadOnlySpan<OverloadParameterType> Wrap(ReadOnlySpan<ITypeSymbol?> types)
    {
        if (types.IsEmpty)
            return default;

        var result = new OverloadParameterType[types.Length];
        for (var i = 0; i < types.Length; i++)
        {
            result[i] = new OverloadParameterType(types[i]);
        }

        return result;
    }

    private static ReadOnlySpan<OverloadParameterType> RemoveNulls(ReadOnlySpan<OverloadParameterType> types)
    {
        var count = 0;
        foreach (var type in types)
        {
            if (type.Symbol is not null)
            {
                count++;
            }
        }

        if (count == types.Length)
            return types;

        var result = new OverloadParameterType[count];
        var index = 0;
        foreach (var type in types)
        {
            if (type.Symbol is not null)
            {
                result[index++] = type;
            }
        }

        return result;
    }

    public bool HasOverloadWithAdditionalParameterOfType(IObjectCreationOperation operation, OverloadOptions options, ReadOnlySpan<ITypeSymbol?> additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(operation, options, additionalParameterTypes) is not null;
    }

    public bool HasOverloadWithAdditionalParameterOfType(IInvocationOperation operation, OverloadOptions options, ReadOnlySpan<ITypeSymbol?> additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(operation, options, additionalParameterTypes) is not null;
    }

    public bool HasOverloadWithAdditionalParameterOfType(IInvocationOperation operation, OverloadOptions options, ReadOnlySpan<OverloadParameterType> additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(operation, options, additionalParameterTypes) is not null;
    }

    public bool HasOverloadWithAdditionalParameterOfType(IMethodSymbol methodSymbol, OverloadOptions options, ReadOnlySpan<ITypeSymbol?> additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(methodSymbol, options, additionalParameterTypes) is not null;
    }

    public bool HasOverloadWithAdditionalParameterOfType(IMethodSymbol methodSymbol, OverloadOptions options, ReadOnlySpan<OverloadParameterType> additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(methodSymbol, options, additionalParameterTypes) is not null;
    }

    public IMethodSymbol? FindOverloadWithAdditionalParameterOfType(IInvocationOperation operation, OverloadOptions options, ReadOnlySpan<ITypeSymbol?> additionalParameterTypes)
    {
        if (options.SyntaxNode is null)
        {
            options = options with { SyntaxNode = operation.Syntax };
        }

        return FindOverloadWithAdditionalParameterOfType(operation.TargetMethod, options, Wrap(additionalParameterTypes));
    }

    public IMethodSymbol? FindOverloadWithAdditionalParameterOfType(IInvocationOperation operation, OverloadOptions options, ReadOnlySpan<OverloadParameterType> additionalParameterTypes)
    {
        if (options.SyntaxNode is null)
        {
            options = options with { SyntaxNode = operation.Syntax };
        }

        return FindOverloadWithAdditionalParameterOfType(operation.TargetMethod, options, additionalParameterTypes);
    }

    public IMethodSymbol? FindOverloadWithAdditionalParameterOfType(IObjectCreationOperation operation, OverloadOptions options, ReadOnlySpan<ITypeSymbol?> additionalParameterTypes)
    {
        if (operation.Constructor is null)
            return null;

        return FindOverloadWithAdditionalParameterOfType(operation.Constructor, options, Wrap(additionalParameterTypes));
    }

    public IMethodSymbol? FindOverloadWithAdditionalParameterOfType(IMethodSymbol methodSymbol, OverloadOptions options, ReadOnlySpan<ITypeSymbol?> additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(methodSymbol, options, Wrap(additionalParameterTypes));
    }

    public IMethodSymbol? FindOverloadWithAdditionalParameterOfType(IMethodSymbol methodSymbol, OverloadOptions options, ReadOnlySpan<OverloadParameterType> additionalParameterTypes)
    {
        additionalParameterTypes = RemoveNulls(additionalParameterTypes);
        if (additionalParameterTypes.IsEmpty)
            return null;

        return FindFirstSimilarMethod(methodSymbol, options, methodSymbol.Name, additionalParameterTypes);
    }

    /// <summary>
    /// Same as <see cref="FindSimilarMethods"/>, but stops on the first matching method instead of collecting all of them.
    /// </summary>
    public IMethodSymbol? FindFirstSimilarMethod(IMethodSymbol methodSymbol, OverloadOptions options, string methodName, ReadOnlySpan<OverloadParameterType> additionalParameterTypes)
    {
        additionalParameterTypes = RemoveNulls(additionalParameterTypes);

        var members = GetCandidateMethods(methodSymbol, methodName, options);
        foreach (var member in members)
        {
            if (IsSimilarMethod(methodSymbol, member, options, additionalParameterTypes, out var method))
                return method;
        }

        return null;
    }

    public ImmutableArray<IMethodSymbol> FindSimilarMethods(IMethodSymbol methodSymbol, OverloadOptions options, string methodName, ReadOnlySpan<OverloadParameterType> additionalParameterTypes)
    {
        additionalParameterTypes = RemoveNulls(additionalParameterTypes);

        List<IMethodSymbol>? result = null;
        var members = GetCandidateMethods(methodSymbol, methodName, options);
        foreach (var member in members)
        {
            if (IsSimilarMethod(methodSymbol, member, options, additionalParameterTypes, out var method))
            {
                result ??= [];
                result.Add(method);
            }
        }

        return result is null ? ImmutableArray<IMethodSymbol>.Empty : ImmutableArray.CreateRange(result);
    }

    private bool IsSimilarMethod(IMethodSymbol methodSymbol, ISymbol member, OverloadOptions options, ReadOnlySpan<OverloadParameterType> additionalParameterTypes, [NotNullWhen(true)] out IMethodSymbol? similarMethod)
    {
        similarMethod = null;
        if (member is not IMethodSymbol method)
            return false;

        if (!options.IncludeObsoleteMembers && IsObsolete(method))
            return false;

        if (!options.IncludeExperimentalMembers && IsExperimental(method))
            return false;

        if (options.ShouldCheckMethod is not null && !options.ShouldCheckMethod(method))
            return false;

        if (!HasSimilarParametersCore(methodSymbol, method, options, additionalParameterTypes))
            return false;

        similarMethod = method;
        return true;
    }

    public bool HasSimilarParameters(IMethodSymbol method, IMethodSymbol otherMethod, bool allowOptionalParameters, params ReadOnlySpan<ITypeSymbol?> additionalParameterTypes)
    {
        return HasSimilarParameters(method, otherMethod, new OverloadOptions(AllowOptionalParameters: allowOptionalParameters), Wrap(additionalParameterTypes));
    }

    /// <summary>
    /// Methods are similar if:
    /// <list type="bullet">
    /// <item><paramref name="method"/> and <paramref name="otherMethod"/> are not the same method</item>
    /// <item><paramref name="method"/> and <paramref name="otherMethod"/> have parameters of the same types, order is not important</item>
    /// <item><paramref name="otherMethod"/> can have additional parameters of type specified by <paramref name="additionalParameterTypes"/></item>
    /// <item>If <paramref name="allowOptionalParameters"/>, <paramref name="otherMethod"/> can have more parameters if they are optional</item>
    /// </list>
    /// </summary>
    public bool HasSimilarParameters(IMethodSymbol method, IMethodSymbol otherMethod, bool allowOptionalParameters, params ReadOnlySpan<OverloadParameterType> additionalParameterTypes)
    {
        return HasSimilarParameters(method, otherMethod, new OverloadOptions(AllowOptionalParameters: allowOptionalParameters), additionalParameterTypes);
    }

    public bool HasSimilarParameters(IMethodSymbol method, IMethodSymbol otherMethod, OverloadOptions options, params ReadOnlySpan<ITypeSymbol?> additionalParameterTypes)
    {
        return HasSimilarParametersCore(method, otherMethod, options, Wrap(additionalParameterTypes));
    }

    public bool HasSimilarParameters(IMethodSymbol method, IMethodSymbol otherMethod, OverloadOptions options, params ReadOnlySpan<OverloadParameterType> additionalParameterTypes)
    {
        return HasSimilarParametersCore(method, otherMethod, options, additionalParameterTypes);
    }

    private bool HasSimilarParametersCore(IMethodSymbol method, IMethodSymbol otherMethod, OverloadOptions options, ReadOnlySpan<OverloadParameterType> additionalParameterTypes)
    {
        if (method.IsEqualTo(otherMethod))
            return false;

        if (!HaveCompatibleGenericSignatures(method, otherMethod))
            return false;

        var methodParameters = GetComparableParameters(method, otherMethod);
        var otherMethodParameters = GetComparableParameters(otherMethod, method);

        // The new method must have at least the same number of parameters as the old method, plus the number of additional parameters
        if (otherMethodParameters.Length - methodParameters.Length < additionalParameterTypes.Length)
            return false;

        // If allowOptionalParameters is false, the new method must have exactly the same number of parameters as the old method
        if (!options.AllowOptionalParameters && otherMethodParameters.Length - methodParameters.Length != additionalParameterTypes.Length)
            return false;

        // The dictionary of inferred type arguments is only allocated when a type argument is actually inferred,
        // so comparing non-generic methods (the most common case) does not allocate.
        var inferredMethodTypeArguments = new InferredTypeArguments();

        // Most of the time, an overload has the same order for the parameters. Try to match them in order first (faster)
        {
            int i = 0, j = 0;
            var additionalParameterIndex = 0;
            while (i < methodParameters.Length && j < otherMethodParameters.Length)
            {
                var methodParameter = methodParameters[i];
                var otherMethodParameter = otherMethodParameters[j];

                if (AreParametersCompatible(methodParameter, otherMethodParameter, method, otherMethod, options, _ienumerableOfTSymbol, _halfSymbol, ref inferredMethodTypeArguments))
                {
                    i++;
                    j++;
                    continue;
                }

                if (additionalParameterIndex == additionalParameterTypes.Length)
                    break;

                var additionalParameter = additionalParameterTypes[additionalParameterIndex];
                if (IsCompatibleWithAdditionalType(otherMethodParameter.Type, additionalParameter))
                {
                    additionalParameterIndex++;
                    j++;
                    continue;
                }

                break;
            }

            if (i == methodParameters.Length && j == otherMethodParameters.Length && additionalParameterIndex == additionalParameterTypes.Length)
                return AreInferredGenericConstraintsSatisfied(method, otherMethod, in inferredMethodTypeArguments);
        }

        // Slower search, allows to find overload with different parameter order
        // Also, handle allow optional parameters
        {
            inferredMethodTypeArguments.Clear();
            var unmatchedOtherMethodParameters = otherMethodParameters;

            foreach (var param in methodParameters)
            {
                var found = false;
                for (var i = 0; i < unmatchedOtherMethodParameters.Length; i++)
                {
                    if (AreParametersCompatible(param, unmatchedOtherMethodParameters[i], method, otherMethod, options, _ienumerableOfTSymbol, _halfSymbol, ref inferredMethodTypeArguments))
                    {
                        unmatchedOtherMethodParameters = unmatchedOtherMethodParameters.RemoveAt(i);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            foreach (var paramType in additionalParameterTypes)
            {
                var found = false;
                for (var i = 0; i < unmatchedOtherMethodParameters.Length; i++)
                {
                    if (IsCompatibleWithAdditionalType(unmatchedOtherMethodParameters[i].Type, paramType))
                    {
                        unmatchedOtherMethodParameters = unmatchedOtherMethodParameters.RemoveAt(i);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            if (unmatchedOtherMethodParameters.Length == 0)
                return AreInferredGenericConstraintsSatisfied(method, otherMethod, in inferredMethodTypeArguments);

            if (options.AllowOptionalParameters)
            {
                if (unmatchedOtherMethodParameters.All(p => p.IsOptional))
                    return true;
            }

            return false;
        }

        bool IsCompatibleWithAdditionalType(ITypeSymbol left, OverloadParameterType right)
        {
            if (right.Symbol is null)
                return false;

            if (right.AllowInherits && left.IsOrInheritsFrom(right.Symbol))
                return true;

            var inferredTypeArguments = new InferredTypeArguments();
            return AreTypesCompatible(
                right.Symbol,
                left,
                method,
                otherMethod,
                options with { AllowNumericConversion = false, AllowInterfaceConversions = false, AllowBaseTypeConversions = false },
                _ienumerableOfTSymbol,
                _halfSymbol,
                ref inferredTypeArguments);
        }

        static bool HaveCompatibleGenericSignatures(IMethodSymbol method, IMethodSymbol otherMethod)
        {
            if (method.IsGenericMethod && !otherMethod.IsGenericMethod)
                return false;

            if (!method.IsGenericMethod)
                return true;

            if (method.Arity != otherMethod.Arity)
                return false;

            for (var i = 0; i < method.Arity; i++)
            {
                var methodTypeParameter = method.TypeParameters[i];
                var otherMethodTypeParameter = otherMethod.TypeParameters[i];

                if (methodTypeParameter.HasReferenceTypeConstraint != otherMethodTypeParameter.HasReferenceTypeConstraint ||
                    methodTypeParameter.HasValueTypeConstraint != otherMethodTypeParameter.HasValueTypeConstraint ||
                    methodTypeParameter.HasNotNullConstraint != otherMethodTypeParameter.HasNotNullConstraint ||
                    methodTypeParameter.HasUnmanagedTypeConstraint != otherMethodTypeParameter.HasUnmanagedTypeConstraint ||
                    methodTypeParameter.HasConstructorConstraint != otherMethodTypeParameter.HasConstructorConstraint ||
                    methodTypeParameter.Variance != otherMethodTypeParameter.Variance ||
                    methodTypeParameter.ConstraintTypes.Length != otherMethodTypeParameter.ConstraintTypes.Length)
                {
                    return false;
                }

                for (var j = 0; j < methodTypeParameter.ConstraintTypes.Length; j++)
                {
                    if (!methodTypeParameter.ConstraintTypes[j].IsEqualTo(otherMethodTypeParameter.ConstraintTypes[j]))
                        return false;
                }
            }

            return true;
        }

        static bool AreParametersCompatible(IParameterSymbol methodParameter, IParameterSymbol otherMethodParameter, IMethodSymbol method, IMethodSymbol otherMethod, OverloadOptions options, ITypeSymbol? ienumerableOfTSymbol, ITypeSymbol? halfSymbol, ref InferredTypeArguments inferredMethodTypeArguments)
        {
            if (!options.AllowParamsToNonParamsCompatibility && methodParameter.IsParams != otherMethodParameter.IsParams)
                return false;

            if (!AreRefKindsCompatible(methodParameter.RefKind, otherMethodParameter.RefKind, options))
                return false;

            return AreTypesCompatible(methodParameter.Type, otherMethodParameter.Type, method, otherMethod, options, ienumerableOfTSymbol, halfSymbol, ref inferredMethodTypeArguments);
        }

        static bool AreRefKindsCompatible(RefKind methodRefKind, RefKind otherMethodRefKind, OverloadOptions options)
        {
            var methodIsByRef = methodRefKind is RefKind.Ref or RefKind.Out;
            var otherMethodIsByRef = otherMethodRefKind is RefKind.Ref or RefKind.Out;

            if (methodIsByRef || otherMethodIsByRef)
                return methodRefKind == otherMethodRefKind;

            if (!options.AllowInModifierCompatibility && (methodRefKind is RefKind.In || otherMethodRefKind is RefKind.In))
                return methodRefKind == otherMethodRefKind;

            // `in` and by-value calls should be treated as compatible for analyzer matching.
            return true;
        }

        static bool AreTypesCompatible(ITypeSymbol methodType, ITypeSymbol otherMethodType, IMethodSymbol method, IMethodSymbol otherMethod, OverloadOptions options, ITypeSymbol? ienumerableOfTSymbol, ITypeSymbol? halfSymbol, ref InferredTypeArguments inferredMethodTypeArguments)
        {
            if (methodType.IsEqualTo(otherMethodType))
                return true;

            if (TryGetMethodTypeArgument(otherMethodType, method, otherMethod, out var mappedType))
                return methodType.IsEqualTo(mappedType);

            if (options.AllowNumericConversion && IsSafeImplicitNumericConversion(methodType, otherMethodType, halfSymbol))
                return true;

            if (methodType is IArrayTypeSymbol methodArrayType &&
                otherMethodType is IArrayTypeSymbol otherMethodArrayType &&
                methodArrayType.Rank == otherMethodArrayType.Rank)
            {
                return AreTypesCompatible(methodArrayType.ElementType, otherMethodArrayType.ElementType, method, otherMethod, options, ienumerableOfTSymbol, halfSymbol, ref inferredMethodTypeArguments);
            }

            if (methodType is not INamedTypeSymbol methodNamedType || otherMethodType is not INamedTypeSymbol otherMethodNamedType)
                return false;

            if (methodNamedType.ConstructedFrom.IsEqualTo(otherMethodNamedType.ConstructedFrom))
            {
                if (methodNamedType.TypeArguments.Length != otherMethodNamedType.TypeArguments.Length)
                    return false;

                for (var i = 0; i < methodNamedType.TypeArguments.Length; i++)
                {
                    var methodTypeArgument = methodNamedType.TypeArguments[i];
                    var otherMethodTypeArgument = otherMethodNamedType.TypeArguments[i];

                    if (TryGetMethodTypeArgument(otherMethodTypeArgument, method, otherMethod, out mappedType))
                    {
                        if (!methodTypeArgument.IsEqualTo(mappedType))
                            return false;

                        continue;
                    }

                    if (!methodTypeArgument.IsEqualTo(otherMethodTypeArgument))
                        return false;
                }

                return true;
            }

            if (IsIEnumerableType(otherMethodNamedType.OriginalDefinition, ienumerableOfTSymbol))
                return false;

            if (options.AllowInterfaceConversions)
            {
                foreach (var candidate in methodNamedType.GetAllInterfacesIncludingSelf().OfType<INamedTypeSymbol>())
                {
                    if (!candidate.OriginalDefinition.IsEqualTo(otherMethodNamedType.OriginalDefinition))
                        continue;

                    if (candidate.TypeArguments.Length != otherMethodNamedType.TypeArguments.Length)
                        continue;

                    var isCompatible = true;
                    for (var i = 0; i < candidate.TypeArguments.Length; i++)
                    {
                        var sourceTypeArgument = candidate.TypeArguments[i];
                        var targetTypeArgument = otherMethodNamedType.TypeArguments[i];
                        if (!AreGenericTypeArgumentsCompatible(sourceTypeArgument, targetTypeArgument, method, otherMethod, ref inferredMethodTypeArguments))
                        {
                            isCompatible = false;
                            break;
                        }
                    }

                    if (isCompatible)
                        return true;
                }
            }

            if (options.AllowBaseTypeConversions &&
                methodNamedType is INamedTypeSymbol directCandidate &&
                directCandidate.BaseType is INamedTypeSymbol baseTypeCandidate &&
                baseTypeCandidate.OriginalDefinition.IsEqualTo(otherMethodNamedType.OriginalDefinition) &&
                baseTypeCandidate.TypeArguments.Length == otherMethodNamedType.TypeArguments.Length)
            {
                var isCompatible = true;
                for (var i = 0; i < baseTypeCandidate.TypeArguments.Length; i++)
                {
                    if (!AreGenericTypeArgumentsCompatible(baseTypeCandidate.TypeArguments[i], otherMethodNamedType.TypeArguments[i], method, otherMethod, ref inferredMethodTypeArguments))
                    {
                        isCompatible = false;
                        break;
                    }
                }

                if (isCompatible)
                    return true;
            }

            return false;
        }

        static bool IsIEnumerableType(INamedTypeSymbol typeSymbol, ITypeSymbol? ienumerableOfTSymbol)
        {
            return ienumerableOfTSymbol is not null && typeSymbol.OriginalDefinition.IsEqualTo(ienumerableOfTSymbol);
        }

        static bool AreGenericTypeArgumentsCompatible(ITypeSymbol sourceTypeArgument, ITypeSymbol targetTypeArgument, IMethodSymbol method, IMethodSymbol otherMethod, ref InferredTypeArguments inferredMethodTypeArguments)
        {
            if (TryGetMethodTypeArgument(targetTypeArgument, method, otherMethod, out var mappedType))
                return sourceTypeArgument.IsEqualTo(mappedType);

            if (targetTypeArgument is ITypeParameterSymbol
                {
                    TypeParameterKind: TypeParameterKind.Method,
                    ContainingSymbol: IMethodSymbol containingMethod,
                } typeParameter
                && containingMethod.IsEqualTo(otherMethod))
            {
                if (inferredMethodTypeArguments.TryGetValue(typeParameter, out var inferredTypeArgument))
                    return sourceTypeArgument.IsEqualTo(inferredTypeArgument);

                inferredMethodTypeArguments.Set(typeParameter, sourceTypeArgument);
                return true;
            }

            return sourceTypeArgument.IsEqualTo(targetTypeArgument);
        }

        static bool AreInferredGenericConstraintsSatisfied(IMethodSymbol sourceMethod, IMethodSymbol targetMethod, in InferredTypeArguments inferredMethodTypeArguments)
        {
            if (!targetMethod.IsGenericMethod)
                return true;

            foreach (var typeParameter in targetMethod.TypeParameters)
            {
                if (!inferredMethodTypeArguments.TryGetValue(typeParameter, out var inferredTypeArgument))
                {
                    if (sourceMethod.IsGenericMethod &&
                        sourceMethod.Arity == targetMethod.Arity &&
                        typeParameter.Ordinal < sourceMethod.TypeArguments.Length)
                    {
                        inferredTypeArgument = sourceMethod.TypeArguments[typeParameter.Ordinal];
                    }
                    else
                    {
                        return false;
                    }
                }

                if (typeParameter.HasReferenceTypeConstraint && !inferredTypeArgument.IsReferenceType)
                    return false;

                if (typeParameter.HasValueTypeConstraint && !inferredTypeArgument.IsValueType)
                    return false;

                if (typeParameter.HasUnmanagedTypeConstraint && !inferredTypeArgument.IsUnmanagedType)
                    return false;

                if (typeParameter.HasConstructorConstraint &&
                    !inferredTypeArgument.IsValueType &&
                    inferredTypeArgument is INamedTypeSymbol namedType &&
                    !namedType.InstanceConstructors.Any(ctor => ctor.Parameters.Length == 0 && ctor.DeclaredAccessibility == Accessibility.Public))
                {
                    return false;
                }

                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    if (!inferredTypeArgument.IsOrInheritsFrom(constraintType) && !inferredTypeArgument.Implements(constraintType))
                        return false;
                }
            }

            return true;
        }

        static bool IsSafeImplicitNumericConversion(ITypeSymbol sourceType, ITypeSymbol targetType, ITypeSymbol? halfSymbol)
        {
            if (sourceType is INamedTypeSymbol namedType &&
                IsMetadataType(namedType, halfSymbol) &&
                targetType.SpecialType is SpecialType.System_Single or SpecialType.System_Double)
            {
                return true;
            }

            return (sourceType.SpecialType, targetType.SpecialType) switch
            {
                (SpecialType.System_SByte, SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
                (SpecialType.System_Byte, SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
                (SpecialType.System_Int16, SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
                (SpecialType.System_UInt16, SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
                (SpecialType.System_Int32, SpecialType.System_Int64 or SpecialType.System_Double or SpecialType.System_Decimal) => true,
                (SpecialType.System_UInt32, SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Double or SpecialType.System_Decimal) => true,
                (SpecialType.System_Int64, SpecialType.System_Decimal) => true,
                (SpecialType.System_UInt64, SpecialType.System_Decimal) => true,
                (SpecialType.System_Char, SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal) => true,
                (SpecialType.System_Single, SpecialType.System_Double) => true,
                _ => false,
            };
        }

        static bool TryGetMethodTypeArgument(ITypeSymbol typeSymbol, IMethodSymbol method, IMethodSymbol otherMethod, [NotNullWhen(true)] out ITypeSymbol? mappedType)
        {
            if (typeSymbol is ITypeParameterSymbol
                {
                    TypeParameterKind: TypeParameterKind.Method,
                    ContainingSymbol: IMethodSymbol containingMethodSymbol,
                } typeParameter
                && containingMethodSymbol.IsEqualTo(otherMethod)
                && typeParameter.Ordinal < method.TypeArguments.Length)
            {
                mappedType = method.TypeArguments[typeParameter.Ordinal];
                return true;
            }

            mappedType = null;
            return false;
        }

        static bool IsMetadataType(INamedTypeSymbol typeSymbol, ITypeSymbol? expectedType)
        {
            return expectedType is not null && typeSymbol.OriginalDefinition.IsEqualTo(expectedType);
        }

        static ImmutableArray<IParameterSymbol> GetComparableParameters(IMethodSymbol method, IMethodSymbol otherMethod)
        {
            if (method.MethodKind is MethodKind.ReducedExtension &&
                method.ReducedFrom is { Parameters.Length: > 0 } reducedFrom)
            {
                return reducedFrom.Parameters.RemoveAt(0);
            }

            if (method.IsExtensionMethod &&
                method.Parameters.Length > 0 &&
                !otherMethod.IsStatic &&
                method.Parameters[0].Type.IsEqualTo(otherMethod.ContainingType))
            {
                return method.Parameters.RemoveAt(0);
            }

            return method.Parameters;
        }
    }

    private List<ISymbol> GetCandidateMethods(IMethodSymbol methodSymbol, string methodName, OverloadOptions options)
    {
        if (methodSymbol.ContainingType is null)
            return [];

        var results = new List<ISymbol>();
        var knownSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        static void AddSymbols(IEnumerable<ISymbol> symbols, List<ISymbol> results, HashSet<ISymbol> knownSymbols)
        {
            foreach (var symbol in symbols)
            {
                if (knownSymbols.Add(symbol))
                {
                    results.Add(symbol);
                }
            }
        }

        var reducedReceiverType = GetReducedReceiverType(methodSymbol);
        if (options.SyntaxNode is not null)
        {
            var semanticModel = compilation.GetSemanticModel(options.SyntaxNode.SyntaxTree);
            var position = options.SyntaxNode.GetLocation().SourceSpan.End;

            AddSymbols(semanticModel.LookupSymbols(position, methodSymbol.ContainingType, methodName, includeReducedExtensionMethods: true), results, knownSymbols);
            if (reducedReceiverType is not null)
            {
                AddSymbols(semanticModel.LookupSymbols(position, reducedReceiverType, methodName, includeReducedExtensionMethods: false), results, knownSymbols);
                AddSymbols(reducedReceiverType.GetMembers(methodName), results, knownSymbols);
            }

            if (options.IncludeExtensionMethodsFromNotImportedNamespaces)
            {
                AddExtensionMethodsFromNotImportedNamespaces(methodSymbol, methodName, semanticModel, position, results);
            }
        }
        else
        {
            AddSymbols(methodSymbol.ContainingType.GetMembers(methodName), results, knownSymbols);
            if (reducedReceiverType is not null)
            {
                AddSymbols(reducedReceiverType.GetMembers(methodName), results, knownSymbols);
            }
        }

        return results;
    }

    /// <summary>
    /// Adds the extension methods that apply to the receiver of <paramref name="methodSymbol"/>, are accessible at
    /// <paramref name="position"/>, and are not in scope because their namespace is not imported. They are added after the
    /// symbols in scope, so a method in scope is found first.
    /// </summary>
    private void AddExtensionMethodsFromNotImportedNamespaces(IMethodSymbol methodSymbol, string methodName, SemanticModel semanticModel, int position, List<ISymbol> results)
    {
        // An extension method applies to the receiver of an instance method, or to the receiver of an extension method
        var receiverType = GetReducedReceiverType(methodSymbol) ?? (methodSymbol.IsStatic ? null : methodSymbol.ContainingType);
        if (receiverType is null || !_extensionMethodsByName.Value.TryGetValue(methodName, out var extensionMethods))
            return;

        HashSet<IMethodSymbol>? extensionMethodsInScope = null;
        foreach (var result in results)
        {
            if (result is IMethodSymbol { ReducedFrom: { } reducedFrom })
            {
                extensionMethodsInScope ??= new(SymbolEqualityComparer.Default);
                extensionMethodsInScope.Add(reducedFrom.OriginalDefinition);
            }
        }

        foreach (var extensionMethod in extensionMethods)
        {
            if (extensionMethodsInScope is not null && extensionMethodsInScope.Contains(extensionMethod.OriginalDefinition))
                continue;

            if (!semanticModel.IsAccessible(position, extensionMethod))
                continue;

            if (extensionMethod.ReduceExtensionMethod(receiverType) is { } reducedMethod)
            {
                results.Add(reducedMethod);
            }
        }
    }

    /// <summary>
    /// Gets the namespace to import at <paramref name="syntaxNode"/> to call <paramref name="methodSymbol"/>, when it is an extension method
    /// declared in a namespace that is not imported, as returned when <see cref="OverloadOptions.IncludeExtensionMethodsFromNotImportedNamespaces"/>
    /// is set. Returns <see langword="null"/> when the method can be called without a new using directive.
    /// </summary>
    public string? GetNamespaceToImport(IMethodSymbol methodSymbol, SyntaxNode syntaxNode)
    {
        if (methodSymbol is not { MethodKind: MethodKind.ReducedExtension, ReducedFrom: { } reducedFrom, ReceiverType: { } receiverType })
            return null;

        var semanticModel = compilation.GetSemanticModel(syntaxNode.SyntaxTree);
        var position = syntaxNode.GetLocation().SourceSpan.End;
        foreach (var symbol in semanticModel.LookupSymbols(position, receiverType, methodSymbol.Name, includeReducedExtensionMethods: true))
        {
            if (symbol is IMethodSymbol { ReducedFrom: { } symbolReducedFrom } && symbolReducedFrom.OriginalDefinition.IsEqualTo(reducedFrom.OriginalDefinition))
                return null;
        }

        return methodSymbol.ContainingNamespace.ToDisplayString();
    }

    /// <summary>
    /// Indexes the extension methods of the compilation and of its references by name. The extension methods declared in
    /// the global namespace are always in scope, so they are not indexed.
    /// </summary>
    private static Dictionary<string, List<IMethodSymbol>> CreateExtensionMethodsByName(Compilation compilation)
    {
        var result = new Dictionary<string, List<IMethodSymbol>>(StringComparer.Ordinal);
        var namespaces = new Stack<INamespaceSymbol>(compilation.GlobalNamespace.GetNamespaceMembers());
        while (namespaces.Count > 0)
        {
            foreach (var member in namespaces.Pop().GetMembers())
            {
                if (member is INamespaceSymbol childNamespace)
                {
                    namespaces.Push(childNamespace);
                }
                else if (member is INamedTypeSymbol { IsStatic: true, MightContainExtensionMethods: true } type)
                {
                    foreach (var typeMember in type.GetMembers())
                    {
                        if (typeMember is not IMethodSymbol { IsExtensionMethod: true } method)
                            continue;

                        if (!result.TryGetValue(method.Name, out var methods))
                        {
                            methods = [];
                            result.Add(method.Name, methods);
                        }

                        methods.Add(method);
                    }
                }
            }
        }

        return result;
    }

    private static ITypeSymbol? GetReducedReceiverType(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.MethodKind is MethodKind.ReducedExtension &&
            methodSymbol.ReducedFrom is { Parameters.Length: > 0 } reducedFromMethod)
        {
            return reducedFromMethod.Parameters[0].Type;
        }

        if (methodSymbol.IsExtensionMethod && methodSymbol.Parameters.Length > 0)
        {
            return methodSymbol.Parameters[0].Type;
        }

        return null;
    }

    private bool IsObsolete(IMethodSymbol methodSymbol)
    {
        if (_obsoleteSymbol is null)
            return false;

        return methodSymbol.HasAttribute(_obsoleteSymbol);
    }

    private bool IsExperimental(IMethodSymbol methodSymbol)
    {
        if (_experimentalSymbol is null)
            return false;

        return methodSymbol.HasAttribute(_experimentalSymbol);
    }

    /// <summary>
    /// Holds the type arguments inferred while comparing two methods. The underlying dictionary is only
    /// allocated when a type argument is inferred, so comparing non-generic methods does not allocate.
    /// </summary>
    private struct InferredTypeArguments
    {
        private Dictionary<ITypeParameterSymbol, ITypeSymbol>? _typeArguments;

        public readonly bool TryGetValue(ITypeParameterSymbol typeParameter, [NotNullWhen(true)] out ITypeSymbol? typeArgument)
        {
            if (_typeArguments is null)
            {
                typeArgument = null;
                return false;
            }

            return _typeArguments.TryGetValue(typeParameter, out typeArgument);
        }

        public void Set(ITypeParameterSymbol typeParameter, ITypeSymbol typeArgument)
        {
            _typeArguments ??= new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
            _typeArguments[typeParameter] = typeArgument;
        }

        public readonly void Clear() => _typeArguments?.Clear();
    }
}
