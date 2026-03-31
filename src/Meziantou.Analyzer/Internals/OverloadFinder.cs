using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Internals;

internal sealed class OverloadFinder(Compilation compilation)
{
    private readonly ITypeSymbol? _obsoleteSymbol = compilation.GetBestTypeByMetadataName("System.ObsoleteAttribute");

    private static ReadOnlySpan<OverloadParameterType> Wrap(ReadOnlySpan<ITypeSymbol?> types)
    {
        var result = new OverloadParameterType[types.Length];
        for (var i = 0; i < types.Length; i++)
        {
            result[i] = new OverloadParameterType(types[i]);
        }

        return result;
    }

    private static ReadOnlySpan<OverloadParameterType> RemoveNulls(ReadOnlySpan<OverloadParameterType> types)
    {
        foreach (var type in types)
        {
            if (type.Symbol is not null)
                continue;

            var list = new List<OverloadParameterType>(types.Length - 1); // We know there is at least one null item
            foreach (var t in types)
            {
                if (t.Symbol is not null)
                {
                    list.Add(t);
                }
            }

            return list.ToArray();
        }

        return types;
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

        var members = GetCandidateMethods(methodSymbol, options);

        foreach (var member in members)
        {
            if (member is IMethodSymbol method)
            {
                if (!options.IncludeObsoleteMembers && IsObsolete(method))
                    continue;

                if (HasSimilarParameters(methodSymbol, method, options.AllowOptionalParameters, additionalParameterTypes))
                    return method;
            }
        }

        return null;
    }

    public static bool HasSimilarParameters(IMethodSymbol method, IMethodSymbol otherMethod, bool allowOptionalParameters, params ReadOnlySpan<ITypeSymbol?> additionalParameterTypes)
    {
        return HasSimilarParameters(method, otherMethod, allowOptionalParameters, Wrap(additionalParameterTypes));
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
    public static bool HasSimilarParameters(IMethodSymbol method, IMethodSymbol otherMethod, bool allowOptionalParameters, params ReadOnlySpan<OverloadParameterType> additionalParameterTypes)
    {
        if (method.IsEqualTo(otherMethod))
            return false;

        var methodParameters = GetComparableParameters(method, otherMethod);
        var otherMethodParameters = GetComparableParameters(otherMethod, method);

        if (!HaveCompatibleGenericSignatures(method, otherMethod))
            return false;

        // The new method must have at least the same number of parameters as the old method, plus the number of additional parameters        
        if (otherMethodParameters.Length - methodParameters.Length < additionalParameterTypes.Length)
            return false;

        // If allowOptionalParameters is false, the new method must have exactly the same number of parameters as the old method
        if (!allowOptionalParameters && otherMethodParameters.Length - methodParameters.Length != additionalParameterTypes.Length)
            return false;

        // Most of the time, an overload has the same order for the parameters. Try to match them in order first (faster)
        {
            var inferredMethodTypeArguments = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
            int i = 0, j = 0;
            var additionalParameterIndex = 0;
            while (i < methodParameters.Length && j < otherMethodParameters.Length)
            {
                var methodParameter = methodParameters[i];
                var otherMethodParameter = otherMethodParameters[j];

                if (AreParametersCompatible(methodParameter, otherMethodParameter, method, otherMethod, inferredMethodTypeArguments))
                {
                    i++;
                    j++;
                    continue;
                }

                if (additionalParameterIndex == additionalParameterTypes.Length)
                    break;

                var additionalParameter = additionalParameterTypes[additionalParameterIndex];
                if (IsEqualTo(methodParameter.Type, additionalParameter))
                {
                    i++;
                    continue;
                }

                if (IsEqualTo(otherMethodParameter.Type, additionalParameter))
                {
                    j++;
                    continue;
                }

                break;
            }

            if (i == methodParameters.Length && j == otherMethodParameters.Length && additionalParameterIndex == additionalParameterTypes.Length)
                return AreInferredGenericConstraintsSatisfied(method, otherMethod, inferredMethodTypeArguments);
        }

        // Slower search, allows to find overload with different parameter order
        // Also, handle allow optional parameters
        {
            var inferredMethodTypeArguments = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
            var unmatchedOtherMethodParameters = otherMethodParameters;

            foreach (var param in methodParameters)
            {
                var found = false;
                for (var i = 0; i < unmatchedOtherMethodParameters.Length; i++)
                {
                    if (AreParametersCompatible(param, unmatchedOtherMethodParameters[i], method, otherMethod, inferredMethodTypeArguments))
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
                    if (IsEqualTo(unmatchedOtherMethodParameters[i].Type, paramType))
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
                return AreInferredGenericConstraintsSatisfied(method, otherMethod, inferredMethodTypeArguments);

            if (allowOptionalParameters)
            {
                if (unmatchedOtherMethodParameters.All(p => p.IsOptional))
                    return true;
            }

            return false;
        }

        static bool IsEqualTo(ITypeSymbol left, OverloadParameterType right)
        {
            return right.AllowInherits
                ? left.IsOrInheritFrom(right.Symbol)
                : left.IsEqualTo(right.Symbol);
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

        static bool AreParametersCompatible(IParameterSymbol methodParameter, IParameterSymbol otherMethodParameter, IMethodSymbol method, IMethodSymbol otherMethod, Dictionary<ITypeParameterSymbol, ITypeSymbol> inferredMethodTypeArguments)
        {
            if (!AreRefKindsCompatible(methodParameter.RefKind, otherMethodParameter.RefKind))
                return false;

            return AreTypesCompatible(methodParameter.Type, otherMethodParameter.Type, method, otherMethod, inferredMethodTypeArguments);
        }

        static bool AreRefKindsCompatible(RefKind methodRefKind, RefKind otherMethodRefKind)
        {
            var methodIsByRef = methodRefKind is RefKind.Ref or RefKind.Out;
            var otherMethodIsByRef = otherMethodRefKind is RefKind.Ref or RefKind.Out;

            if (methodIsByRef || otherMethodIsByRef)
                return methodRefKind == otherMethodRefKind;

            // `in` and by-value calls should be treated as compatible for analyzer matching.
            return true;
        }

        static bool AreTypesCompatible(ITypeSymbol methodType, ITypeSymbol otherMethodType, IMethodSymbol method, IMethodSymbol otherMethod, Dictionary<ITypeParameterSymbol, ITypeSymbol> inferredMethodTypeArguments)
        {
            if (methodType.IsEqualTo(otherMethodType))
                return true;

            if (TryGetMethodTypeArgument(otherMethodType, method, otherMethod, out var mappedType))
                return methodType.IsEqualTo(mappedType);

            if (IsSafeImplicitNumericConversion(methodType, otherMethodType))
                return true;

            if (methodType is IArrayTypeSymbol methodArrayType &&
                otherMethodType is IArrayTypeSymbol otherMethodArrayType &&
                methodArrayType.Rank == otherMethodArrayType.Rank)
            {
                return AreTypesCompatible(methodArrayType.ElementType, otherMethodArrayType.ElementType, method, otherMethod, inferredMethodTypeArguments);
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

            if (IsIEnumerableType(otherMethodNamedType.OriginalDefinition))
                return false;

            foreach (var candidate in methodNamedType.GetAllInterfacesIncludingThis().OfType<INamedTypeSymbol>())
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
                    if (!AreGenericTypeArgumentsCompatible(sourceTypeArgument, targetTypeArgument, method, otherMethod, inferredMethodTypeArguments))
                    {
                        isCompatible = false;
                        break;
                    }
                }

                if (isCompatible)
                    return true;
            }

            if (methodNamedType is INamedTypeSymbol directCandidate &&
                directCandidate.BaseType is INamedTypeSymbol baseTypeCandidate &&
                baseTypeCandidate.OriginalDefinition.IsEqualTo(otherMethodNamedType.OriginalDefinition) &&
                baseTypeCandidate.TypeArguments.Length == otherMethodNamedType.TypeArguments.Length)
            {
                var isCompatible = true;
                for (var i = 0; i < baseTypeCandidate.TypeArguments.Length; i++)
                {
                    if (!AreGenericTypeArgumentsCompatible(baseTypeCandidate.TypeArguments[i], otherMethodNamedType.TypeArguments[i], method, otherMethod, inferredMethodTypeArguments))
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

        static bool IsIEnumerableType(INamedTypeSymbol typeSymbol)
        {
            return IsMetadataType(typeSymbol, "System.Collections.Generic.IEnumerable`1");
        }

        static bool AreGenericTypeArgumentsCompatible(ITypeSymbol sourceTypeArgument, ITypeSymbol targetTypeArgument, IMethodSymbol method, IMethodSymbol otherMethod, Dictionary<ITypeParameterSymbol, ITypeSymbol> inferredMethodTypeArguments)
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

                inferredMethodTypeArguments[typeParameter] = sourceTypeArgument;
                return true;
            }

            return sourceTypeArgument.IsEqualTo(targetTypeArgument);
        }

        static bool AreInferredGenericConstraintsSatisfied(IMethodSymbol sourceMethod, IMethodSymbol targetMethod, Dictionary<ITypeParameterSymbol, ITypeSymbol> inferredMethodTypeArguments)
        {
            if (!targetMethod.IsGenericMethod)
                return true;

            foreach (var typeParameter in targetMethod.TypeParameters)
            {
                ITypeSymbol? inferredTypeArgument = null;
                if (!inferredMethodTypeArguments.TryGetValue(typeParameter, out inferredTypeArgument))
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
                    if (!inferredTypeArgument.IsOrInheritFrom(constraintType) && !inferredTypeArgument.Implements(constraintType))
                        return false;
                }
            }

            return true;
        }

        static bool IsSafeImplicitNumericConversion(ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            if (sourceType is INamedTypeSymbol namedType &&
                IsMetadataType(namedType, "System.Half") &&
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

        static bool IsMetadataType(INamedTypeSymbol typeSymbol, string metadataName)
        {
            var expectedType = typeSymbol.ContainingAssembly?.GetTypeByMetadataName(metadataName);
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

    private ImmutableArray<ISymbol> GetCandidateMethods(IMethodSymbol methodSymbol, OverloadOptions options)
    {
        if (methodSymbol.ContainingType is null)
            return ImmutableArray<ISymbol>.Empty;

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

            AddSymbols(semanticModel.LookupSymbols(position, methodSymbol.ContainingType, methodSymbol.Name, includeReducedExtensionMethods: true), results, knownSymbols);
            if (reducedReceiverType is not null)
            {
                AddSymbols(semanticModel.LookupSymbols(position, reducedReceiverType, methodSymbol.Name, includeReducedExtensionMethods: false), results, knownSymbols);
            }
        }
        else
        {
            AddSymbols(methodSymbol.ContainingType.GetMembers(methodSymbol.Name), results, knownSymbols);
            if (reducedReceiverType is not null)
            {
                AddSymbols(reducedReceiverType.GetMembers(methodSymbol.Name), results, knownSymbols);
            }
        }

        return ImmutableArray.CreateRange(results);
    }

    private static ITypeSymbol? GetReducedReceiverType(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.MethodKind is not MethodKind.ReducedExtension || methodSymbol.ReducedFrom is null || methodSymbol.ReducedFrom.Parameters.Length == 0)
            return null;

        return methodSymbol.ReducedFrom.Parameters[0].Type;
    }

    private bool IsObsolete(IMethodSymbol methodSymbol)
    {
        if (_obsoleteSymbol is null)
            return false;

        return methodSymbol.HasAttribute(_obsoleteSymbol);
    }
}
